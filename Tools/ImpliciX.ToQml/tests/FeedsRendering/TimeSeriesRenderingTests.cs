using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class TimeSeriesRenderingTests
{
    [TestCaseSource(nameof(_feedsExpectations))]
    public void Id(Expectation expect)
    {
        var renderer = new TimeSeriesRenderer();
        Assert.That(renderer.Id(expect.Feed), Is.EqualTo(expect.ExpectedId));
        
    }
    
    [TestCaseSource(nameof(_feedsExpectations))]
    public void RawValue(Expectation expect)
    {
        var renderer = new TimeSeriesRenderer();
        Assert.That(renderer.GetValueOf(expect.Feed.InCache().RawValue), Is.EqualTo(expect.InCache+"values"));
        Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").RawValue), Is.EqualTo(expect.OutOfCache+"values"));
    }
    
    [TestCaseSource(nameof(_feedsExpectations))]
    public void FormattedValue(Expectation expect)
    {
        var renderer = new TimeSeriesRenderer();
        Assert.That(renderer.GetValueOf(expect.Feed.InCache().Formatted), Is.EqualTo(expect.InCache+"getFormattedValues()"));
        Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").Formatted), Is.EqualTo(expect.OutOfCache+"getFormattedValues()"));
    }
    
    [TestCaseSource(nameof(_feedsExpectations))]
    public void SetValue(Expectation expect)
    {
        var renderer = new TimeSeriesRenderer();
        Assert.That(renderer.SetValueOf(expect.Feed.InCache(), "foo"), Is.EqualTo(expect.InCache+"setDataPointsFromString(foo);"));
        Assert.That(renderer.SetValueOf(expect.Feed.OutOfCache("theCache"), "foo"), Is.EqualTo(expect.OutOfCache+"setDataPointsFromString(foo);"));
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

        public static Expectation Create(string name, string expectedId) =>
            new(TimeSeriesFeed.Subscribe(Urn.BuildUrn("root", name)), expectedId);
    }
    
    private static TestCaseData[] _feedsExpectations =
    {
        new (Expectation.Create("temperature",$"{TimeSeriesRenderer.CacheUrnPrefix}$root$temperature")),
        new (Expectation.Create("a:b:_c:d", $"{TimeSeriesRenderer.CacheUrnPrefix}$root$a$b$_c$d")),
    };
}