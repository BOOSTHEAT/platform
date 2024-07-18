using System;
using System.CommandLine;
using System.Linq;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class OptionsExtensionsTests
{
  [TestCase("a","Version number a is not valid.")]
  [TestCase("1.","Version number 1. is not valid.")]
  [TestCase("","Required argument missing for option: '-v'.")]
  [TestCase("1.2.3","Version number 1.2.3 is not valid.")]
  [TestCase("1.2.3.4.5","Version number 1.2.3.4.5 is not valid.")]
  [TestCase("1.2.blob.4","Version number 1.2.blob.4 is not valid.")]
  [TestCase("1.2.3.4","")]
  public void VersionNumberArgument(string version, string expectedAnswer)
  {
    var rootCommand = new RootCommand
    {
      new Option<string>(new[] { "-v" }, "Version number").VersionNumberOnly()
    };
    var console = new Helpers.SpyConsole();
    rootCommand.Invoke(Helpers.GetArgs($"-v {version}"), console);
    Assert.That(console.AllText().Split('\n', StringSplitOptions.TrimEntries).First(), Is.EqualTo(expectedAnswer));
  }
  
  [TestCase("A",true)]
  [TestCase("AB",true)]
  [TestCase("AB.CD",true)]
  [TestCase("AB.CD.EeeeeF",true)]
  [TestCase("AB.",false)]
  [TestCase("AB.CD.",false)]
  [TestCase(".AB",false)]
  public void CodeIdentifierArgument(string value, bool isOk)
  {
    var rootCommand = new RootCommand
    {
      new Option<string>(new[] { "-i" }, "Identifier").CodeIdentifier()
    };
    var console = new Helpers.SpyConsole();
    rootCommand.Invoke(Helpers.GetArgs($"-i {value}"), console);
    Assert.That(console.AllText().Length == 0, Is.EqualTo(isOk));
  }
}