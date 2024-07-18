using System;
using System.IO;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services.SshInfrastructure;

internal interface ISftpClient : IDisposable
{
  Task Upload(string source, string destination);
  Task Upload(Stream source, string destination);
  Task Download(string source, string destination);
  Task<string> Download(string source);
}