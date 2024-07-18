using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services.SshInfrastructure;

internal interface ISshClient : IDisposable
{
  void ForwardPort(string boundHost, uint boundPort, string host, uint port);
  Task<string> Execute(string command);
  Task Execute(string command, string destination);
}

internal static class SshClientExtensions
{
  private const int HEADER_ELEMENT_COUNT_WHEN_HAS_CHECKSUM = 2;
  public const string HEADER_ELEMENT_SEPARATOR = "<SEP>";
  public const string HEADER_COMMAND_SEPARATOR = "\t";

  public static async IAsyncEnumerable<(int count, int length, string name, string checksum)> ExecuteMany(
    this ISshClient ssh, string source, string destinationFolder)
  {
    const StringSplitOptions cleanEntries = StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries;
    var inputResult = await ssh.Execute(source);
    var inputs = inputResult.Split('\n', cleanEntries);
    var count = 1;

    foreach (var input in inputs)
    {
      var op = input.Split(HEADER_COMMAND_SEPARATOR, cleanEntries);

      var header = op[0];
      var cmd = op[1];
      var remoteFileChecksum = "";
      var fileName = header;

      //TODO : Maybe set checksum mandatory for all feature that use this method
      var dataHeader = header.Split(HEADER_ELEMENT_SEPARATOR, cleanEntries);
      if (dataHeader.Length >= HEADER_ELEMENT_COUNT_WHEN_HAS_CHECKSUM)
      {
        remoteFileChecksum = dataHeader[0];
        fileName = dataHeader[1];
      }

      var outputFileFullPath = Path.Combine(destinationFolder, fileName);

      yield return (count++, inputs.Length, fileName, remoteFileChecksum);
      await ssh.Execute(cmd, outputFileFullPath);
    }
  }
}
