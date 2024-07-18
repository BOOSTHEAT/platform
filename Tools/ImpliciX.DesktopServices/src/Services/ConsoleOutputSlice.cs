using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services;

internal class ConsoleOutputSlice : IConsoleOutputSlice
{
  private readonly IConsoleService _console;
  private readonly List<string> _lines;

  public ConsoleOutputSlice(IConsoleService console)
  {
    _console = console;
    _lines = new List<string>();
    _console.LineWritten += OnConsoleOnLineWritten;
    _console.Errors += OnConsoleOnErrors;
  }

  public async Task DumpInto(Stream stream)
  {
    await using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);
    foreach (var line in _lines)
      await writer.WriteLineAsync(line);
  }

  public async Task DumpInto(string filePath)
  {
    await using var stream = File.Create(filePath);
    await DumpInto(stream);
  }

  public void Dispose()
  {
    _console.LineWritten -= OnConsoleOnLineWritten;
    _console.Errors -= OnConsoleOnErrors;
  }

  private void OnConsoleOnErrors(object sender, Exception e) => _lines.Add(e.Message);
  private void OnConsoleOnLineWritten(object sender, string line) => _lines.Add(line);
}
