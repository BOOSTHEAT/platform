using ImpliciX.Data.TimeSeries;
using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries.Tests;

public class SelfDefinedSeriesTests
{
  private SelfDefinedSeries _sut = null!;

  [SetUp]
  public void Init()
  {
    _sut = new SelfDefinedSeries(new []
    {
      Series("a", "a:foo", "a:bar").Over.ThePast(5).Days,
      Series("b", "b").Over.ThePast(3).Hours,
    });
  }

  [Test]
  public void RootUrns()
  {
    Assert.That(_sut.RootUrns.Select(u => u.Value), Is.EqualTo(new [] {"a", "b"}));
  }

  [Test]
  public void ContainsRootUrn()
  {
    Assert.True(_sut.ContainsRootUrn("a"));
    Assert.True(_sut.ContainsRootUrn("b"));
    Assert.False(_sut.ContainsRootUrn("c"));
  }

  [Test]
  public void StorablePropertiesForRoot()
  {
    Assert.That(GetStorablePropertiesForRoot("a"), Is.EqualTo((new [] {"a:foo", "a:bar"}, TimeSpan.FromDays(5))));
    Assert.That(GetStorablePropertiesForRoot("b"), Is.EqualTo((new [] {"b"}, TimeSpan.FromHours(3))));
    Assert.That(GetStorablePropertiesForRoot("c"), Is.EqualTo((Array.Empty<string>(), TimeSpan.Zero)));
    
    (string[], TimeSpan) GetStorablePropertiesForRoot(string root)
    {
      var result = _sut.StorablePropertiesForRoot(root);
      return (result.Item1.Select(u => u.Value).ToArray(), result.Item2);
    }
  }
  
  [Test]
  public void OutputUrns()
  {
    Assert.That(_sut.OutputUrns.Select(u => u.Value), Is.EqualTo(new [] {"a:foo", "a:bar", "b"}));
  }
  
  [Test]
  public void RootUrnOf()
  {
    Assert.That(_sut.RootUrnOf("a:foo")!.Value, Is.EqualTo("a"));
    Assert.That(_sut.RootUrnOf("a:bar")!.Value, Is.EqualTo("a"));
    Assert.That(_sut.RootUrnOf("b")!.Value, Is.EqualTo("b"));
    Assert.That(_sut.RootUrnOf("c"), Is.Null);
  }
  
  ITimeSeries Series(string urn, params string[] fields) =>
    new TimeSeries(Urn.BuildUrn(urn), fields.Select(f => Urn.BuildUrn(f)).ToArray());
  
}