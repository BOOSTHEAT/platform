using System.Threading.Tasks;
using Renci.SshNet;

namespace ImpliciX.DesktopServices;

public interface IIdentity
{
  string Name { get; }
  Task<(PrivateKeyFile File, string Key)?> Create();
  Task<(PrivateKeyFile File, string Key)?> Import(string key);
  Task<(PrivateKeyFile File, string Key)?> Read();
  Task<string> ReadAsString();
}