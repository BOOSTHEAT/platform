using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Tools;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests;

public class UrnExtensionsTests
{
  private Urn[] _urns = new []
  {
    Urn.BuildUrn("foo","bar","a","b"),
    Urn.BuildUrn("foo","bar","a","c"),
    Urn.BuildUrn("foo","bar","d"),
    Urn.BuildUrn("foo","bar","d","e"),
    Urn.BuildUrn("foo","bar","f"),
    Urn.BuildUrn("foo","qix"),
    Urn.BuildUrn("plop"),
  };
  
  [TestCase(0,1,"foo:bar:a:b")]
  [TestCase(0,2,"foo:bar:a")]
  [TestCase(2,2,"foo:bar:d")]
  [TestCase(0,5,"foo:bar")]
  [TestCase(0,6,"foo")]
  [TestCase(0,7,"")]
  public void ComputeRoot(int start, int size, string expected)
  {
    var root = _urns.Skip(start).Take(size).FindRoot();
    Assert.That(root, Is.EqualTo(Urn.BuildUrn(expected)));
  }

}