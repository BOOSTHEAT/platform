using ImpliciX.Linker.Values;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests.Values;

public class ExeInstallTests
{
  [TestCase("foo,/opt/software/bar", "foo", "/opt/software/bar")]
  public void Parse(string definition, string expectedUrn, string expectedPath)
  {
    Assert.IsFalse(ExeInstall.IsInvalid(definition));
    var fr = new ExeInstall(definition);
    Assert.That(fr.Urn, Is.EqualTo(expectedUrn));
    Assert.That(fr.Path, Is.EqualTo(expectedPath));
  }
  
  [TestCase("x")]
  [TestCase("x,y,z")]
  public void Invalid(string definition)
  {
    Assert.IsTrue(ExeInstall.IsInvalid(definition));
  }
}