using ImpliciX.Linker.Values;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests.Values;

public class FeedReferenceTests
{
  [TestCase("xx", "xx", "*")]
  [TestCase("yy", "yy", "*")]
  [TestCase("xx:1.*", "xx", "1.*")]
  [TestCase("xx:1.2.*", "xx", "1.2.*")]
  [TestCase("xx:1.2.3.*", "xx", "1.2.3.*")]
  [TestCase("xx:1.2.3.4", "xx", "1.2.3.4")]
  [TestCase("yy:1.2.3.4", "yy", "1.2.3.4")]
  public void Parse(string definition, string expectedName, string expectedVersion)
  {
    Assert.IsFalse(FeedReference.IsInvalid(definition));
    var fr = new FeedReference(definition);
    Assert.That(fr.Name, Is.EqualTo(expectedName));
    Assert.That(fr.Version, Is.EqualTo(expectedVersion));
  }
  
  [TestCase("xx:")]
  [TestCase("xx:a")]
  [TestCase("xx:1..")]
  public void Invalid(string definition)
  {
    Assert.IsTrue(FeedReference.IsInvalid(definition));
  }
}