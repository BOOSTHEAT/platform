using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;
using Moq;
using NUnit.Framework;
using TestsModel = ImpliciX.TimeMath.Tests.Helpers.fake_analytics_model;

namespace ImpliciX.TimeMath.Tests;

public static class TimeMathFactoryHelper
{
  internal const string InputUrn = "temperature";

  internal const string FakeAnalytics = "fake_analytics";
  internal const string Measure = "measure";
  internal const string Variation = "variation";

  internal static readonly string AccumulatedUrn = MetricUrn.BuildAccumulatedValue();
  internal static readonly string CountUrn = MetricUrn.BuildSamplesCount();

  internal static readonly MetricUrn TemperatureUrn = TestsModel.temperature;
  internal static readonly string TemperatureResult = TemperatureUrn;
  internal static readonly MetricUrn TemperatureUrnAccumulated = MetricUrn.BuildAccumulatedValue(TemperatureResult);
  internal static readonly MetricUrn TemperatureUrnCount = MetricUrn.BuildSamplesCount(TemperatureResult);

  internal static readonly MetricUrn TemperatureInputUrn = MetricUrn.Build(
    FakeAnalytics,
    nameof(TemperatureInputUrn)
  );

  internal static readonly MetricUrn TemperatureMeasureUrn =
    MetricUrn.Build(
      FakeAnalytics,
      nameof(TemperatureInputUrn),
      Measure
    );

  internal static readonly string TemperatureMeasure = TemperatureMeasureUrn;


  public static void ReaderReturnLastUpdateAt(
    Mock<ITimeMathReader> readerMock,
    string rootUrn,
    string suffix
  )
  {
    ReaderReturnLastUpdateAt(
        readerMock,
        rootUrn,
        suffix,
        Option<FloatValueAt>.None()
      )
      ;
  }

  public static void ReaderReturnLastUpdateAt(
    Mock<ITimeMathReader> readerMock,
    string rootUrn,
    string suffix,
    TimeSpan updateAt,
    float value
  )
  {
    ReaderReturnLastUpdateAt(
        readerMock,
        rootUrn,
        suffix,
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            updateAt,
            value
          )
        )
      )
      ;
  }

  public static void ReaderReturnLastUpdateAt(
    Mock<ITimeMathReader> readerMock,
    string rootUrn,
    string suffix,
    Option<FloatValueAt> result
  )
  {
    readerMock
      .Setup(
        reader => reader.ReadLastUpdate(
          rootUrn,
          suffix
        )
      )
      .Callback(
        (
            string _rootUrn,
            string _suffix
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {_rootUrn} + {_suffix}")
      )
      .Returns(result)
      ;
  }

  public static void ReaderReturnFirstValueAtPublish(
    Mock<ITimeMathReader> readerMock,
    string rootUrn,
    string suffix,
    TimeSpan updateAt,
    float value
  )
  {
    ReaderReturnFirstValueAtPublish(
        readerMock,
        rootUrn,
        suffix,
        updateAt,
        Option<FloatValueAt>.Some(
          new FloatValueAt(
            updateAt,
            value
          )
        )
      )
      ;
  }

  public static void ReaderReturnFirstValueAtPublish(
    Mock<ITimeMathReader> readerMock,
    string rootUrn,
    string suffix,
    TimeSpan start,
    Option<FloatValueAt> result
  )
  {
    readerMock
      .Setup(
        reader => reader.ReadFirstValueAtPublish(
          rootUrn,
          suffix,
          start
        )
      )
      .Callback(
        (
            string _rootUrn,
            string _suffix,
            TimeSpan at
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {_rootUrn} + {_suffix} at {at}")
      )
      .Returns(result)
      ;
  }

  public static void ReaderReturnEndAt(
    Mock<ITimeMathReader> readerMock,
    string urn,
    TimeSpan result
  )
  {
    readerMock
      .Setup(reader => reader.ReadEndAt(urn))
      .Callback(
        (
            string urn
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {urn}")
      )
      .Returns(result)
      ;
  }

  public static void ReaderReturnLastPublishAt(
    Mock<ITimeMathReader> readerMock,
    string urn,
    TimeSpan result
  )
  {
    readerMock
      .Setup(reader => reader.ReadLastPublishedInstant(urn))
      .Callback(
        (
            string urn
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {urn}")
      )
      .Returns(Option<TimeSpan>.Some(result))
      ;
  }

  public static void ReaderReturnStartAt(
    Mock<ITimeMathReader> readerMock,
    string urn,
    TimeSpan result
  )
  {
    readerMock
      .Setup(reader => reader.ReadStartAt(urn))
      .Callback(
        (
            string urn
          ) =>
          TestContext.Progress.WriteLine($"Incoming call: {urn}")
      )
      .Returns(result)
      ;
  }
}
