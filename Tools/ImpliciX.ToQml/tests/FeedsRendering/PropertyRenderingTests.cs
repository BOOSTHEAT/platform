using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class PropertyRenderingTests
{
  [TestCaseSource(nameof(_feedsExpectations))]
  public void Id(Expectation expect)
  {
    var renderer = new PropertyRenderer();
    Assert.That(renderer.Id(expect.Feed), Is.EqualTo(expect.ExpectedId));
  }
  
  [TestCaseSource(nameof(_feedsExpectations))]
  public void RawValue(Expectation expect)
  {
    var renderer = new PropertyRenderer();
    Assert.That(renderer.GetValueOf(expect.Feed.InCache().RawValue), Is.EqualTo(expect.InCache+"value"));
    Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").RawValue), Is.EqualTo(expect.OutOfCache+"value"));
  }
  
  [TestCaseSource(nameof(_feedsExpectations))]
  public void FormattedValue(Expectation expect)
  {
    var renderer = new PropertyRenderer();
    Assert.That(renderer.GetValueOf(expect.Feed.InCache().Formatted), Is.EqualTo(expect.InCache+"display()"));
    Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").Formatted), Is.EqualTo(expect.OutOfCache+"display()"));
  }
  
  [TestCaseSource(nameof(_feedsExpectations))]
  public void SetValue(Expectation expect)
  {
    var renderer = new PropertyRenderer();
    Assert.That(renderer.SetValueOf(expect.Feed.InCache(), "foo"), Is.EqualTo(expect.InCache+"value = foo;"));
    Assert.That(renderer.SetValueOf(expect.Feed.OutOfCache("theCache"), "foo"), Is.EqualTo(expect.OutOfCache+"value = foo;"));
  }

  public class Expectation
  {
    public Expectation(Feed feed, string expectedId)
    {
      Feed = feed;
      ExpectedId = expectedId;
      InCache = expectedId + ".";
      OutOfCache = "theCache." + InCache;
    }

    public readonly Feed Feed;
    public readonly string OutOfCache;
    public readonly string InCache;
    public readonly string ExpectedId;

    public static Expectation Create<T>(string name, string expectedId) =>
      new(PropertyFeed.Subscribe(PropertyUrn<T>.Build("root", name)), expectedId);
  }

  private static TestCaseData[] _feedsExpectations = new[]
  {
    new TestCaseData(Expectation.Create<Temperature>("temperature","root$temperature")),
    new TestCaseData(Expectation.Create<Pressure>("pressure", "root$pressure")),
    new TestCaseData(Expectation.Create<Power>("power", "root$power")),
    new TestCaseData(Expectation.Create<SoftwareVersion>("whatever", "root$whatever")),
  };
}