using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class GenericCommandTests
{
  [Test]
  public void SimpleString()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-s foo"), _console);
    Assert.That(_spyArgs["str"], Is.EqualTo("foo"));
  }
  
  [Test]
  public void ExistingFile()
  {
    _rootCommand!.Invoke(Helpers.GetArgs($"-e {Assembly.GetExecutingAssembly().Location}"), _console);
    Assert.That(((FileInfo)_spyArgs["existing"]).FullName, Is.EqualTo(Assembly.GetExecutingAssembly().Location));
  }

  [Test]
  public void CustomValue()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-v foo"), _console);
    Assert.That(((MyValue)_spyArgs["value"]).Definition, Is.EqualTo("foo"));
  }
  
  [Test]
  public void SimpleStrings()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-x foo -x bar"), _console);
    Assert.That(_spyArgs["xs"], Is.EqualTo(new [] {"foo","bar"}));
  }
  
  [Test]
  public void ExistingFiles()
  {
    _rootCommand!.Invoke(Helpers.GetArgs($"-f {Assembly.GetExecutingAssembly().Location} -f {Assembly.GetCallingAssembly().Location}"), _console);
    Assert.That(
      ((List<FileInfo>)_spyArgs["files"]).Select(f => f.FullName),
      Is.EqualTo(new [] {Assembly.GetExecutingAssembly().Location,Assembly.GetCallingAssembly().Location})
      );
  }
    
  [Test]
  public void MultipleCustomValues()
  {
    _rootCommand!.Invoke(Helpers.GetArgs("-m foo -m bar"), _console);
    Assert.That(((List<MyValue>)_spyArgs["multi"]).Select(x => x.Definition), Is.EqualTo(new [] {"foo","bar"}));
  }
  
  [SetUp]
  public void Setup()
  {
    _rootCommand = new RootCommand("the root command")
    {
      new Option<FileInfo>(
        new[] { "-e", "--existing" },
        "Some existing file").ExistingOnly(),
      new Option<string>(
        new[] { "-s", "--str" },
        "Some string"),
      new Option<MyValue>(
        new[] { "-v", "--value" },
        "Some custom value"),
      new Option<FileInfo>(
        new[] { "-f", "--files" },
        "Some existing files"){Arity = ArgumentArity.ZeroOrMore}.ExistingOnly(),
      new Option<string>(
        new[] { "-x", "--xs" },
        "Some strings"){Arity = ArgumentArity.ZeroOrMore},
      new Option<MyValue>(
        new[] { "-m", "--multi" },
        "Some multiple custom value"){Arity = ArgumentArity.ZeroOrMore}

    };
    var _ = new MyCommand(_rootCommand, this);
    _console = new Helpers.SpyConsole();
  }

  class MyValue
  {
    public MyValue(string definition)
    {
      Definition = definition;
    }
    public string Definition { get; }
  }

  class MyCommand : GenericCommand
  {
    public MyCommand(Command command, GenericCommandTests self) : base(command) => _self = self;
    private readonly GenericCommandTests _self;
    protected override int Execute(Dictionary<string, object> arguments)
    {
      _self._spyArgs = arguments;
      return 0;
    }
  }

  private RootCommand? _rootCommand;
  private Helpers.SpyConsole? _console;
  private Dictionary<string, object> _spyArgs = new ();
}