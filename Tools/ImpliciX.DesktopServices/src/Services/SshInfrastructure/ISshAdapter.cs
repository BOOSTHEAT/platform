using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services.SshInfrastructure;

internal interface ISshAdapter
{
  Task<bool> IsPingable(string ipAddressOrHostname);
  ISshClient CreateClient(string host, int port, string username, ISshIdentity identity);
  ISftpClient CreateSftpClient(string host, int port, string username, ISshIdentity identity);
  Task<ISshIdentity> LoadIdentity(IIdentity identity);
  IEnumerable<UnicastIPAddressInformation> ForwardableUnicasts { get; }
}