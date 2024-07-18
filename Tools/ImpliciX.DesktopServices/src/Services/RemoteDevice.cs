using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using ImpliciX.Data.Api;
using ImpliciX.DesktopServices.Services.SshInfrastructure;
using ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

namespace ImpliciX.DesktopServices.Services;

internal class RemoteDevice : IRemoteDevice
{
  public const int WebSocketLocalPort = 9999;
  private readonly Func<SshClientFactory, string, IConsoleOutputSlice, Task<ITargetSystem>> _connectedDeviceFactory;
  private readonly RemoteDeviceHistory _connectionHistory;
  private readonly IConsoleService _console;
  private readonly Subject<IRemoteDeviceDefinition> _deviceDefinition;
  private readonly IIdentity _implicixIdentity;
  private readonly Subject<bool> _isConnected;
  private readonly ISshAdapter _sshAdapter;
  private readonly SshClientFactory _sshClientFactory;
  private readonly Subject<ITargetSystem> _targetSystem;

  private readonly IWebsocketAdapter _wsAdapter;
  private string _deviceHost;
  private uint _devicePort;
  private IDisposable _disposables;
  private ISshIdentity _sshIdentity = null;
  private IWebsocketClient _wsClient;

  public RemoteDevice(
    IConsoleService console,
    IIdentity identity,
    ISshAdapter sshAdapter,
    IWebsocketAdapter wsAdapter,
    Func<SshClientFactory, string, IConsoleOutputSlice, Task<ITargetSystem>> connectedDeviceFactory)
  {
    IPAddressOrHostname = string.Empty;
    Properties = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    LocalIPAddresses = Enumerable.Empty<string>();
    _isConnected = new();
    _isConnected.OnNext(false);
    _console = console;
    _implicixIdentity = identity;
    _sshAdapter = sshAdapter;
    LocalIPAddresses = _sshAdapter.ForwardableUnicasts.Select(u => u.Address.ToString()).ToArray();
    _wsAdapter = wsAdapter;
    _connectedDeviceFactory = connectedDeviceFactory;
    _connectionHistory = new();
    _sshClientFactory = new SshClientFactory(async () =>
    {
      await LoadIdentity();
      return _sshAdapter.CreateClient(_deviceHost, (int)_devicePort, "root", _sshIdentity);
    }, async () =>
    {
      await LoadIdentity();
      return _sshAdapter.CreateSftpClient(_deviceHost, (int)_devicePort, "root", _sshIdentity);
    });
    _targetSystem = new Subject<ITargetSystem>();
    _deviceDefinition = new Subject<IRemoteDeviceDefinition>();
  }
  public IObservable<bool> IsConnected => _isConnected;
  public string IPAddressOrHostname { get; private set; }
  public SourceCache<ImpliciXProperty, string> Properties { get; }
  public IEnumerable<string> LocalIPAddresses { get; private set; }
  public ITargetSystem CurrentTargetSystem { get; private set; }
  public IObservable<ITargetSystem> TargetSystem => _targetSystem;
  public IObservable<IRemoteDeviceDefinition> DeviceDefinition => _deviceDefinition;

  public async IAsyncEnumerable<string> Suggestions(string partOfIpAddressOrHostname)
  {
    foreach (var target in _connectionHistory.Get(partOfIpAddressOrHostname))
    {
      yield return target;
    }
  }

  public async Task Connect(string ipAddressOrHostname)
  {
    using var consoleSlice = IConsoleOutputSlice.Create(_console);
    try
    {
      Log($"Connecting to {ipAddressOrHostname}");
      IPAddressOrHostname = ipAddressOrHostname;
      CurrentTargetSystem = await SetupTargetSystem(ipAddressOrHostname, consoleSlice);
      _connectionHistory.Add(ipAddressOrHostname);
      _targetSystem.OnNext(CurrentTargetSystem);
      await StartWsClient(CurrentTargetSystem.Address, WebSocketLocalPort);
      Log($"Connected on port {WebSocketLocalPort}");
      _isConnected.OnNext(true);
    }
    catch (WebsocketConnectionFailure e)
    {
      await CurrentTargetSystem!.FixAppConnection();
    }
    catch (Exception e)
    {
      await Disconnect(e);
    }
  }

  public async Task Disconnect(Exception e = null)
  {
    if (IPAddressOrHostname == string.Empty)
      return;
    if (e != null)
      _console.WriteError(e);
    Log($"Disconnected from {IPAddressOrHostname}\n");
    Properties.Clear();
    CurrentTargetSystem = null;
    _targetSystem.OnNext(CurrentTargetSystem);
    _deviceDefinition.OnNext(null);
    IPAddressOrHostname = string.Empty;
    _wsClient?.Dispose();
    _wsClient = null;
    if (_disposables != null)
    {
      Log("Tearing down SSH Connection");
      _disposables.Dispose();
      _disposables = null;
      Log($"SSH Connection Teardown complete");
    }
    _isConnected.OnNext(false);
    Log("--------------------------------");
  }

  public async Task<bool> Send(string json)
  {
    if (_wsClient == null || !_wsClient.IsConnected)
    {
      Log($"Cannot send {json}");
      return false;
    }

    Log($"Sending {json}");
    return await _wsClient.Send(json);
  }

  public async Task Upload(string source, string destination)
  {
    Log($"Uploading '{source}' to '{destination}'");
    using var sftp = await _sshClientFactory.CreateSftpClient();
    await sftp.Upload(source, destination);
    Log("Uploading complete");
  }

  private async Task<ITargetSystem> SetupTargetSystem(string ipAddressOrHostname,
    IConsoleOutputSlice consoleOutputSlice)
  {
    if (IPAddress.TryParse(ipAddressOrHostname, out IPAddress address) && IsLocal(address))
      return new LoopbackTargetSystem(ipAddressOrHostname);
    await SetupSshTunnel(ipAddressOrHostname);
    return await _connectedDeviceFactory(_sshClientFactory, ipAddressOrHostname, consoleOutputSlice);
  }

  private bool IsLocal(IPAddress address) =>
    Equals(address, IPAddress.Loopback) || IsLocalDockerHeuristic(address);

  private bool IsLocalDockerHeuristic(IPAddress address) =>
    address.AddressFamily == AddressFamily.InterNetwork && address.GetAddressBytes()[0] == 172;

  private async Task SetupSshTunnel(string ipAddressOrHostname)
  {
    var isIpAddress = IPAddress.TryParse(ipAddressOrHostname, out IPAddress _);
    if (isIpAddress)
    {
      var ipAddress = ipAddressOrHostname;
      await SetupDirectSshTunnel(ipAddress);
      return;
    }

    var hostname = ipAddressOrHostname;
    var forcedJumpHost = Environment.GetEnvironmentVariable("BH_SSH_JUMP");
    if (!string.IsNullOrEmpty(forcedJumpHost))
    {
      await SetupProxifiedSshTunnel(hostname, forcedJumpHost);
      return;
    }

    if (await _sshAdapter.IsPingable(hostname))
    {
      Log($"Ping success for hostname {hostname}");
      await SetupDirectSshTunnel(hostname);
      return;
    }

    var defaultJumpHost = "remotemoe.boostheat.org";
    await SetupProxifiedSshTunnel($"{hostname}.vm-remotemoe-prod", defaultJumpHost);
  }

  private async Task StartWsClient(string ipAddress, int port)
  {
    _wsClient = _wsAdapter.CreateClient(2000, ipAddress, port);
    _wsClient.MessageReceived += (sender, json) =>
    {
      try
      {
        UpdateFromJson(json, _deviceDefinition, Properties, Log);
      }
      catch (Exception e)
      {
        Log($"Error while processing {json}: ${e.Message}");
      }
    };
    _wsClient.ConnectionLost += async (sender, b) => await Disconnect();
    await _wsClient.Start();
  }

  public static void UpdateFromJson(string json, IObserver<IRemoteDeviceDefinition> definitions,
    ISourceCache<ImpliciXProperty, string> properties, Action<string> log)
  {
    WebsocketApiV2
      .FromJson(json)
      .OnPrelude(p =>
      {
        definitions.OnNext(new RemoteDeviceDefinition(p));
      })
      .OnProperties(p =>
        properties.AddOrUpdate(p
          .Where(x =>
          {
            if (x.Value != null)
              return true;
            log($"Unexpected null value for {x.Urn}");
            return false;
          })
          .Select(x => new ImpliciXProperty(x.Urn, Convert.ToString(x.Value, CultureInfo.InvariantCulture)))
        )
      );
  }

  private void Log(string msg)
  {
    _console.WriteLine(msg);
  }

  private async Task SetupProxifiedSshTunnel(string virtualHostname, string jumpHost)
  {
    Log($"Setting up Proxified SSH Connection to {virtualHostname} through {jumpHost}");
    LocalIPAddresses = _sshAdapter.ForwardableUnicasts.Select(u => u.Address.ToString()).ToArray();
    _deviceHost = "127.0.0.1";
    _devicePort = 8222;
    var proxyJump = await SetupProxyJump(jumpHost, virtualHostname);
    var deviceConnection = await SetupDeviceConnection(LocalIPAddresses);
    _disposables = new CompositeDisposable(deviceConnection, proxyJump);
    Log($"Proxified SSH Connection setup complete");
  }

  private async Task SetupDirectSshTunnel(string ipAddressOrHostname)
  {
    Log($"Setting up Direct SSH Connection to {ipAddressOrHostname}");
    LocalIPAddresses = _sshAdapter.ForwardableUnicasts.Select(u => u.Address.ToString()).ToArray();
    _deviceHost = ipAddressOrHostname;
    _devicePort = 22;
    var deviceConnection = await SetupDeviceConnection(LocalIPAddresses);
    _disposables = new CompositeDisposable(deviceConnection);
    Log("Direct SSH Connection setup complete");
  }

  private async Task<IDisposable> SetupDeviceConnection(IEnumerable<string> localAddresses)
  {
    var tunnel = await _sshClientFactory.CreateSshClient();
    var forwardings = new (uint locallyBound, uint forwarded)[]
    {
      (WebSocketLocalPort, 9999),
      (8086, 8086),
      (6379, 6379),
      (9222, 22),
      (9502, 502),
      (4849, 4849),
      (4850, 4850),
      (4851, 4851),
      (4852, 4852),
      (4853, 4853),
      (4854, 4854),
      (4855, 4855),
      (4856, 4856),
      (5283, 5283)
    };
    foreach (var port in forwardings)
    {
      foreach (var localAddress in localAddresses)
      {
        try
        {
          tunnel.ForwardPort(localAddress, port.locallyBound, "127.0.0.1", port.forwarded);
        }
        catch (Exception)
        {
          Log(
            $"Error while forwarding remote port {port.forwarded} on local address/port {localAddress}:{port.locallyBound}");
        }
      }
    }

    return tunnel;
  }

  private async Task<IDisposable> SetupProxyJump(string jumpHost, string targetHost)
  {
    await LoadIdentity();
    var proxyJump = _sshAdapter.CreateClient(jumpHost, 2222, "whatever", _sshIdentity);
    proxyJump.ForwardPort(_deviceHost, _devicePort, targetHost, 22);
    return proxyJump;
  }

  private async Task LoadIdentity()
  {
    if (_sshIdentity == null)
    {
      _sshIdentity = await _sshAdapter.LoadIdentity(_implicixIdentity);
      Log($"Connecting with identity {_sshIdentity.PublicKey}");
    }
  }
}
