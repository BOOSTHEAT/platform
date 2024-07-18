using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class SpikeCommandLine
{

  [Test]
  public void SimpleString()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-s foo"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the root command"));
    Assert.That(_context!.ParseResult.GetValueForOption(GetOption("Some string")), Is.EqualTo("foo"));
  }
  
  [Test]
  public void ExistingFile()
  {
    _rootCommand!.Invoke(Helpers.GetArgs($"-e {Assembly.GetExecutingAssembly().Location}"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the root command"));
    var fileInfo = (FileInfo)_context!.ParseResult.GetValueForOption(GetOption("Some existing file"))!;
    Assert.That(fileInfo.Exists, Is.True);
    Assert.That(fileInfo.Name, Is.EqualTo("ImpliciX.Linker.Tests.dll"));
  }
  
  [Test]
  public void Verb()
  {
    _rootCommand!.Invoke(Helpers.GetArgs($"fizz"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the fizz verb"));
  }
  
  [Test]
  public void OtherVerb()
  {
    _rootCommand!.Invoke(Helpers.GetArgs($"buzz"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the buzz verb"));
  }

  [Test]
  public void VerbWithLocalOption()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("fizz -f bar"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the fizz verb"));
    Assert.That(_context!.ParseResult.GetValueForOption(GetOption("Some fizz string")), Is.EqualTo("bar"));
  }

  [Test]
  public void VerbWithGlobalOption()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-s foo fizz"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the fizz verb"));
    Assert.That(_context!.ParseResult.GetValueForOption(GetOption("Some string")), Is.EqualTo("foo"));
  }

  [Test]
  public void VerbWithBothOptions()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-s foo fizz -f bar"), _console);
    Assert.That(_console!.AllText(), Is.Empty);
    Assert.That(UsedCommand.Description, Is.EqualTo("the fizz verb"));
    Assert.That(_context!.ParseResult.GetValueForOption(GetOption("Some string")), Is.EqualTo("foo"));
    Assert.That(_context!.ParseResult.GetValueForOption(GetOption("Some fizz string")), Is.EqualTo("bar"));
  }

  private Option GetOption(string description) => (Option)_symbols![description];
  private ICommand UsedCommand => _context!.ParseResult.CommandResult.Command;
  private RootCommand? _rootCommand;
  private InvocationContext? _context;
  private Helpers.SpyConsole? _console;
  private Dictionary<string, ISymbol>? _symbols;


  [SetUp]
  public void Setup()
  {
    _rootCommand = new RootCommand("the root command")
    {
      Helpers.CreateCommand(
        "fizz",
        "the fizz verb",
        new GenericHandler(this),
        new Option<string>(
          new[] { "-f", "--fizzstr" },
          "Some fizz string")
      ),
      Helpers.CreateCommand(
        "buzz",
        "the buzz verb",
        new GenericHandler(this),
        new Option<string>(
          new[] { "-b", "--buzzstr" },
          "Some buzz string")
      ),
      new Option<FileInfo>(
        new[] { "-e", "--existing" },
        "Some existing file").ExistingOnly(),
      new Option<string>(
        new[] { "-s", "--str" },
        "Some string")
    };
    _rootCommand.Handler = new GenericHandler(this);
    _console = new Helpers.SpyConsole();
    _symbols = Helpers.GetAllDescendants(_rootCommand).ToDictionary(s => s.Description!, s => s);
  }

  private class GenericHandler : ICommandHandler
  {
    private readonly SpikeCommandLine _spikeCommandLine;

    public GenericHandler(SpikeCommandLine spikeCommandLine)
    {
      _spikeCommandLine = spikeCommandLine;
    }
    
    public Task<int> InvokeAsync(InvocationContext context)
    {
      _spikeCommandLine._context = context;
      return Task.FromResult(0);
    }
  }
}