using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class MeasureRenderingTests
{
  public class Expectation
  {
    public Expectation(Feed feed, string expectedId, string callLocalFormatted, string callLocalRaw)
    {
      Feed = feed;
      ExpectedId = expectedId;
      var expectedInCache = expectedId + ".";
      var expectedOutOfCache = "theCache." + expectedInCache;
      OutOfCacheLocalRawValue = expectedOutOfCache + callLocalRaw;
      InCacheLocalRawValue = expectedInCache + callLocalRaw;
      OutOfCacheLocalFormattedValue = expectedOutOfCache + callLocalFormatted;
      InCacheLocalFormattedValue = expectedInCache + callLocalFormatted;
      var callNeutralFormatted = "getFormattedValue(Unit.none,true)";
      var callNeutralRaw = "getValue(Unit.none)";
      OutOfCacheNeutralRawValue = expectedOutOfCache + callNeutralRaw;
      InCacheNeutralRawValue = expectedInCache + callNeutralRaw;
      OutOfCacheNeutralFormattedValue = expectedOutOfCache + callNeutralFormatted;
      InCacheNeutralFormattedValue = expectedInCache + callNeutralFormatted;
    }

    public readonly Feed Feed;
    public readonly string OutOfCacheLocalRawValue;
    public readonly string InCacheLocalRawValue;
    public readonly string OutOfCacheLocalFormattedValue;
    public readonly string InCacheLocalFormattedValue;
    public readonly string OutOfCacheNeutralRawValue;
    public readonly string InCacheNeutralRawValue;
    public readonly string OutOfCacheNeutralFormattedValue;
    public readonly string InCacheNeutralFormattedValue;
    public readonly string ExpectedId;

    public static Expectation Create<T>(string name, string expectedId,
      string callLocalFormatted, string callLocalRaw)
      => new(
        MeasureFeed.Subscribe(new MeasureNode<T>(Urn.BuildUrn(name), new root())),
        expectedId,
        callLocalFormatted, callLocalRaw);
  }

  private static TestCaseData[] _feedsExpectations2 = new[]
  {
    new TestCaseData(Expectation.Create<Temperature>("temperature",
      "root$temperature", "getFormattedValue(Unit.toCelsius,true)", "getValue(Unit.toCelsius)")),
    new TestCaseData(Expectation.Create<Pressure>("pressure", 
      "root$pressure", "getFormattedValue(Unit.toBar,true)", "getValue(Unit.toBar)")),
    new TestCaseData(Expectation.Create<Power>("power", 
      "root$power", "getFormattedValue(Unit.toKw,true)", "getValue(Unit.toKw)")),
    new TestCaseData(Expectation.Create<Energy>("energy", 
      "root$energy", "getFormattedValue(Unit.toKwh,true)", "getValue(Unit.toKwh)")),
    new TestCaseData(Expectation.Create<Percentage>("percentage", 
      "root$percentage", "getFormattedValue(Unit.toPercentage,true)", "getValue(Unit.toPercentage)")),
    new TestCaseData(Expectation.Create<SoftwareVersion>("whatever", 
      "root$whatever", "getFormattedValue(Unit.none,true)", "getValue(Unit.none)")),
  };

  [TestCaseSource(nameof(_feedsExpectations2))]
  public void Id(Expectation expect)
  {
    var renderer = new MeasureRenderer();
    Assert.That(renderer.Id(expect.Feed), Is.EqualTo(expect.ExpectedId));
  }

  [TestCaseSource(nameof(_feedsExpectations2))]
  public void LocalRawValue(Expectation expect)
  {
    var renderer = new MeasureRenderer();
    Assert.That(renderer.GetValueOf(expect.Feed.InCache().UseLocalSettings.RawValue), Is.EqualTo(expect.InCacheLocalRawValue));
    Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").UseLocalSettings.RawValue), Is.EqualTo(expect.OutOfCacheLocalRawValue));
  }

  [TestCaseSource(nameof(_feedsExpectations2))]
  public void LocalFormattedValue(Expectation expect)
  {
    var renderer = new MeasureRenderer();
    Assert.That(renderer.GetValueOf(expect.Feed.InCache().UseLocalSettings.Formatted), Is.EqualTo(expect.InCacheLocalFormattedValue));
    Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").UseLocalSettings.Formatted), Is.EqualTo(expect.OutOfCacheLocalFormattedValue));
  }

  [TestCaseSource(nameof(_feedsExpectations2))]
  public void NeutralRawValue(Expectation expect)
  {
    var renderer = new MeasureRenderer();
    Assert.That(renderer.GetValueOf(expect.Feed.InCache().UseNeutralSettings.RawValue), Is.EqualTo(expect.InCacheNeutralRawValue));
    Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").UseNeutralSettings.RawValue), Is.EqualTo(expect.OutOfCacheNeutralRawValue));
  }

  [TestCaseSource(nameof(_feedsExpectations2))]
  public void NeutralFormattedValue(Expectation expect)
  {
    var renderer = new MeasureRenderer();
    Assert.That(renderer.GetValueOf(expect.Feed.InCache().UseNeutralSettings.Formatted), Is.EqualTo(expect.InCacheNeutralFormattedValue));
    Assert.That(renderer.GetValueOf(expect.Feed.OutOfCache("theCache").UseNeutralSettings.Formatted), Is.EqualTo(expect.OutOfCacheNeutralFormattedValue));
  }
}