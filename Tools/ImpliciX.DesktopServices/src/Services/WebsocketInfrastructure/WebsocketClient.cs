using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

internal class WebsocketClient : IWebsocketClient
{
  private readonly int _timeout;
  private readonly string _ipAddress;
  private readonly int _port;
  private WatsonWsClient _client;

  public WebsocketClient(int timeout, string ipAddress, int port)
  {
    _timeout = timeout;
    _ipAddress = ipAddress;
    _port = port;
  }

  public void Dispose()
  {
    _client?.Dispose();
  }

  public Task Start()
  {
    var accepted = false;
    const int interval = 50;
    var remaining = _timeout/interval;
    void TryCreate(Action<WatsonWsClient> accept, Action reject)
    {
      _client?.Dispose();
      _client = new WatsonWsClient(_ipAddress, _port, false);
      _client.MessageReceived += (sender, args) => OnMessage(args);
      _client.ServerConnected += (sender, args) =>
      {
        accepted = true;
        accept(_client);
      };
      _client.ServerDisconnected += (sender, args) =>
      {
        if (accepted)
        {
          OnDisconnection();
          return;
        }
        if (remaining-- < 0)
        {
          reject();
          return;
        }
        Thread.Sleep(interval);
        TryCreate(accept, reject);
      };
      _client.Start();
    }
    var tcs = new TaskCompletionSource();
    TryCreate(
      client => tcs.SetResult(),
      () => tcs.SetException(new WebsocketConnectionFailure($"Cannot connect to websocket {_ipAddress}:{_port}"))
    );
    return tcs.Task;
  }

  public bool IsConnected => _client is { Connected: true };

  public async Task<bool> Send(string json)
  {
    if (!IsConnected)
      return false;
    return await _client.SendAsync(json);
  }

  public event EventHandler<string> MessageReceived;
  public event EventHandler<bool> ConnectionLost;

  internal void OnMessage(MessageReceivedEventArgs args)
  {
    MessageReceived?.Invoke(this, Encoding.UTF8.GetString(args.Data));
  }

  internal void OnDisconnection()
  {
    ConnectionLost?.Invoke(this, true);
  }
}