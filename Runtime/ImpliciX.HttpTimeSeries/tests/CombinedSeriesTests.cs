using ImpliciX.Data.TimeSeries;
using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries.Tests;

public class CombinedSeriesTests
{
  private CombinedSeries _sut = null!;

  [SetUp]
  public void Init()
  {
    _sut = new CombinedSeries(
      new SelfDefinedSeries(new[]
      {
        Series("a", "a:foo", "a:bar").Over.ThePast(5).Days,
        Series("b", "b").Over.ThePast(3).Hours,
      }),
      new SelfDefinedSeries(new[]
      {
        Series("c", "c:foo", "c:bar").Over.ThePast(8).Days,
        Series("d", "d").Over.ThePast(2).Hours,
      })
    );
  }

  [Test]
  public void RootUrns()
  {
    Assert.That(_sut.RootUrns.Select(u => u.Value), Is.EqualTo(new[] { "a", "b", "c", "d" }));
  }

  [Test]
  public void ContainsRootUrn()
  {
    Assert.True(_sut.ContainsRootUrn("a"));
    Assert.True(_sut.ContainsRootUrn("b"));
    Assert.True(_sut.ContainsRootUrn("c"));
    Assert.True(_sut.ContainsRootUrn("d"));
    Assert.False(_sut.ContainsRootUrn("e"));
  }

  [Test]
  public void StorablePropertiesForRoot()
  {
    Assert.That(GetStorablePropertiesForRoot("a"), Is.EqualTo((new[] { "a:foo", "a:bar" }, TimeSpan.FromDays(5))));
    Assert.That(GetStorablePropertiesForRoot("b"), Is.EqualTo((new[] { "b" }, TimeSpan.FromHours(3))));
    Assert.That(GetStorablePropertiesForRoot("c"), Is.EqualTo((new[] { "c:foo", "c:bar" }, TimeSpan.FromDays(8))));
    Assert.That(GetStorablePropertiesForRoot("d"), Is.EqualTo((new[] { "d" }, TimeSpan.FromHours(2))));
    Assert.That(GetStorablePropertiesForRoot("e"), Is.EqualTo((Array.Empty<string>(), TimeSpan.Zero)));

    (string[], TimeSpan) GetStorablePropertiesForRoot(string root)
    {
      var result = _sut.StorablePropertiesForRoot(root);
      return (result.Item1.Select(u => u.Value).ToArray(), result.Item2);
    }
  }

  [Test]
  public void OutputUrns()
  {
    Assert.That(_sut.OutputUrns.Select(u => u.Value), Is.EqualTo(new[]
    {
      "a:foo", "a:bar", "b", "c:foo", "c:bar", "d",
    }));
  }

  [Test]
  public void RootUrnOf()
  {
    Assert.That(_sut.RootUrnOf("a:foo")!.Value, Is.EqualTo("a"));
    Assert.That(_sut.RootUrnOf("a:bar")!.Value, Is.EqualTo("a"));
    Assert.That(_sut.RootUrnOf("b")!.Value, Is.EqualTo("b"));
    Assert.That(_sut.RootUrnOf("c:foo")!.Value, Is.EqualTo("c"));
    Assert.That(_sut.RootUrnOf("c:bar")!.Value, Is.EqualTo("c"));
    Assert.That(_sut.RootUrnOf("d")!.Value, Is.EqualTo("d"));
    Assert.That(_sut.RootUrnOf("e"), Is.Null);
  }

  ITimeSeries Series(string urn, params string[] fields) =>
    new TimeSeries(Urn.BuildUrn(urn), fields.Select(f => Urn.BuildUrn(f)).ToArray());
}