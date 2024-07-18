using System;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;

namespace ImpliciX.DesktopServices.Services.SshInfrastructure;

internal class SshNetClient : ISshClient
{
  private readonly SshClient _client;

  public SshNetClient(SshClient client)
  {
    _client = client;
  }

  public void Dispose()
  {
    _client.Dispose();
  }

  public void ForwardPort(string boundHost, uint boundPort, string host, uint port)
  {
    var forwardedPort = new ForwardedPortLocal(boundHost, boundPort, host, port);
    _client.AddForwardedPort(forwardedPort);
    forwardedPort.Start();
  }

  public async Task<string> Execute(string command)
  {
    using var cmd = _client.CreateCommand(command);
    await Task.Factory.FromAsync(cmd.BeginExecute(), cmd.EndExecute);
    if (cmd.ExitStatus == 0)
      return cmd.Result;
    throw new ApplicationException(cmd.Error);
  }

  public async Task Execute(string command, string destination)
  {
    Directory.CreateDirectory(
      Path.GetDirectoryName(destination)
      ?? throw new InvalidOperationException($"No folder name for {destination}")
    );
    using var cmd = _client.CreateCommand(command);
    await Task.Factory.FromAsync(cmd.BeginExecute(), async iar =>
    {
      var br = new BinaryReader(cmd.OutputStream);
      var bytes = new byte[cmd.OutputStream.Length];
      var read = br.Read(bytes);
      await File.WriteAllBytesAsync(destination, bytes);
      cmd.EndExecute(iar);
    });
  }
}
