using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Linq;

namespace ImpliciX.Linker.Tests;

public class Helpers
{
  public static string[] GetArgs(string args) => args.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
  public static IEnumerable<ISymbol> GetAllDescendants(IEnumerable<ISymbol> symbols) =>
    symbols.SelectMany(s => s is IEnumerable<ISymbol> childSymbols ? GetAllDescendants(childSymbols).Prepend(s) : new[] { s });

  public static Command CreateCommand(string name, string description, ICommandHandler handler, params Option[] options)
  {
    var cmd = new Command(name, description);
    foreach (var option in options)
      cmd.Add(option);
    cmd.Handler = handler;
    return cmd;
  }


  public class SpyConsole : IConsole
  {
    public string AllText() => string.Join("",StdErr.Text)+string.Join("",StdOut.Text);
    public readonly StreamToString StdOut = new();
    public readonly StreamToString StdErr = new();
    public SpyConsole()
    {
      Out = StdOut;
      Error = StdErr;
    }

    public IStandardStreamWriter Out { get; }
    public bool IsOutputRedirected => true;
    public IStandardStreamWriter Error { get; }
    public bool IsErrorRedirected => true;
    public bool IsInputRedirected => true;
  }

  public class StreamToString : IStandardStreamWriter
  {
    public List<string> Text { get; } = new();
    public void Write(string value) => Text.Add(value);
  }
}