using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Renci.SshNet;

namespace ImpliciX.DesktopServices.Services.SshInfrastructure;

internal class SshNetAdapter : ISshAdapter
{
  public async Task<bool> IsPingable(string ipAddressOrHostname)
  {
    try
    {
      var pingReply = await new Ping().SendPingAsync(ipAddressOrHostname, 50);
      return pingReply!.Status == IPStatus.Success;
    }
    catch (Exception)
    {
      return false;
    }
  }

  public ISshClient CreateClient(string host, int port, string username, ISshIdentity identity)
  {
    var client = CreateClient<SshClient>(host, port, username, identity, ci => new SshClient(ci));
    return new SshNetClient(client);
  }

  public ISftpClient CreateSftpClient(string host, int port, string username, ISshIdentity identity)
  {
    var client = CreateClient<SftpClient>(host, port, username, identity, ci => new SftpClient(ci));
    return new SshNetSftpClient(client);
  }
  
  private T CreateClient<T>(
    string host, int port, string username, ISshIdentity identity, Func<ConnectionInfo, BaseClient> create)
    where T : BaseClient
  {
    var keyFile = ((SshIdentity)identity).KeyFile;
    var connectionInfo = new ConnectionInfo(host,
      port,
      username,
      new PrivateKeyAuthenticationMethod(username, keyFile))
    {
      Timeout = TimeSpan.FromSeconds(3)
    };
    var client = create(connectionInfo);
    client.Connect();
    return (T) client;
  }
  
  public IEnumerable<UnicastIPAddressInformation> ForwardableUnicasts =>
    (from adapter in NetworkInterface.GetAllNetworkInterfaces()
      let properties = adapter.GetIPProperties()
      from unicast in properties.UnicastAddresses
      where unicast.Address.AddressFamily == AddressFamily.InterNetwork
            && (Environment.OSVersion.Platform == PlatformID.Unix || unicast.SuffixOrigin != SuffixOrigin.LinkLayerAddress)
      select unicast).ToArray();

  public async Task<ISshIdentity> LoadIdentity(IIdentity identity)
  {
    var keyFile = (await identity.Read())!.Value.File;
    return new SshIdentity(keyFile);
  }

  class SshIdentity : ISshIdentity
  {
    public SshIdentity(PrivateKeyFile keyFile)
    {
      KeyFile = keyFile;
    }

    internal PrivateKeyFile KeyFile { get; }
    public string PublicKey => KeyFile.ToPublic();
  }
}