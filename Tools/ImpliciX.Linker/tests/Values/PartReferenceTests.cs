using System.Reflection;
using ImpliciX.Linker.Values;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests.Values;

public class PartReferenceTests
{
  [TestCaseSource(nameof(_nominalCases))]
  public void Parse(string definition, string expectedId, string expectedVersion, string expectedPath,
    string expectedCategory)
  {
    Assert.IsFalse(PartReference.IsInvalid(definition));
    var pr = new PartReference(definition);
    Assert.That(pr.Id, Is.EqualTo(expectedId));
    Assert.That(pr.Version, Is.EqualTo(expectedVersion));
    Assert.That(pr.Path.FullName, Is.EqualTo(expectedPath));
    Assert.That(pr.Category, Is.EqualTo(expectedCategory));
  }

  private static object[] _nominalCases =
  {
    new[]
    {
      $"a,2.1.*,{Assembly.GetExecutingAssembly().Location}", "a", "2.1.*", Assembly.GetExecutingAssembly().Location, "APPS"
    },
    new[]
    {
      $"foo:bar:qix,3.216.25.42,{Assembly.GetCallingAssembly().Location}", "foo:bar:qix", "3.216.25.42",
      Assembly.GetCallingAssembly().Location, "APPS"
    },
    new[]
    {
      $"foo:bar:qix  ,  3.216.* , {Assembly.GetCallingAssembly().Location}", "foo:bar:qix", "3.216.*",
      Assembly.GetCallingAssembly().Location, "APPS"
    },
    new[]
    {
      $"foo:bar:qix,3.216.25.42,{Assembly.GetCallingAssembly().Location},LOL", "foo:bar:qix", "3.216.25.42",
      Assembly.GetCallingAssembly().Location, "LOL"
    },
  };
  
  [TestCaseSource(nameof(_invalidCases))]
  public void Invalid(string reason, string definition)
  {
    Assert.IsTrue(PartReference.IsInvalid(definition), $"Should fail because: {reason}");
  }

  private static object[] _invalidCases =
  {
    new[] { "Missing data", $"xx" },
    new[] { "Missing data", $"x,2.1" },
    new[] { "Non existing file", $"x,2.1,v" },
    new[] { "Invalid version number", $"x,2.3.4,{Assembly.GetExecutingAssembly().Location},u" },
    new[] { "Invalid version number", $"x,1.2.3.4.5,{Assembly.GetExecutingAssembly().Location},u" },
    new[] { "Invalid version number", $"x,1.*.2,{Assembly.GetExecutingAssembly().Location},u" },
    new[] { "Too much data", $"x,2.1,{Assembly.GetExecutingAssembly().Location},u,v" },
  };
}