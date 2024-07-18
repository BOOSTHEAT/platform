using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;

namespace ImpliciX.DesktopServices.Services.SshInfrastructure;

internal class SshNetSftpClient : ISftpClient
{
  private readonly SftpClient _client;
  
  public SshNetSftpClient(SftpClient client)
  {
    _client = client;
  }

  public void Dispose()
  {
    _client.Dispose();
  }

  public async Task Upload(string source, string destination)
  {
    await using var input = File.OpenRead(source);
    await Task.Factory.FromAsync(_client.BeginUploadFile(input, destination), _client.EndUploadFile);
  }

  public async Task Upload(Stream source, string destination)
  {
    await Task.Factory.FromAsync(_client.BeginUploadFile(source, destination), _client.EndUploadFile);
  }

  public async Task Download(string source, string destination)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(destination));
    await using var output = File.OpenWrite(destination);
    await Task.Factory.FromAsync(_client.BeginDownloadFile(source, output), _client.EndDownloadFile);
  }

  public async Task<string> Download(string source)
  {
    var output = new MemoryStream();
    await Task.Factory.FromAsync(_client.BeginDownloadFile(source, output), _client.EndDownloadFile);
    output.Seek(0, SeekOrigin.Begin);
    var reader = new StreamReader(output);
    var result = await reader.ReadToEndAsync();
    return result;
  }
}