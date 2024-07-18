using System;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Services.SshInfrastructure;

namespace ImpliciX.DesktopServices.Services;

internal class SshClientFactory
{
  public Task<ISshClient> CreateSshClient() => _createSshClient();
  private readonly Func<Task<ISshClient>> _createSshClient;

  public Task<ISftpClient> CreateSftpClient() => _createSftpClient();
  private readonly Func<Task<ISftpClient>> _createSftpClient;

  public SshClientFactory(
    Func<Task<ISshClient>> createSshClient,
    Func<Task<ISftpClient>> createSftpClient
    )
  {
    _createSshClient = createSshClient;
    _createSftpClient = createSftpClient;
  }
}