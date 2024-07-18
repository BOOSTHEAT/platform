using ImpliciX.Linker.Values;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests.Values;

public class SourceAndTargetTests
{
  [TestCase("/foo/bar,/fizz/buzz", "/foo/bar", "/fizz/buzz")]
  public void Parse(string definition, string expectedSource, string expectedTarget)
  {
    Assert.IsFalse(ExeInstall.IsInvalid(definition));
    var fr = new SourceAndTarget(definition);
    Assert.That(fr.Source.FullName, Is.EqualTo(expectedSource));
    Assert.That(fr.Target.FullName, Is.EqualTo(expectedTarget));
  }
  
  [TestCase("x")]
  [TestCase("x,y,z")]
  public void Invalid(string definition)
  {
    Assert.IsTrue(ExeInstall.IsInvalid(definition));
  }
}