using System;
using System.Linq;
using FluentAssertions;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TestsCommon;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Tests.Helpers;
using Moq;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TimeMath.Tests.TimeMathFactoryHelper;
using TimeUnit = ImpliciX.Language.Model.TimeUnit;

namespace ImpliciX.TimeMath.Tests;

[TestFixture]
public class TimeMathServiceTest
{
  [SetUp]
  public void Init()
  {
    var fakeTimeMathPersistence = new FakeTimeMathPersistence();
    _writer = fakeTimeMathPersistence;
    _reader = fakeTimeMathPersistence;
    _timeMathService = new TimeMathService(() => T._0);
  }

  private const int MsInSeconds = 1000;
  private const int MsInMinutes = 60 * MsInSeconds;
  private static readonly TimeHelper T = TimeHelper.Minutes();
  private readonly TimeSpan _3Minutes = T._3;
  private TimeMathService _timeMathService;
  private ITimeMathWriter _writer;
  private ITimeMathReader _reader;

  [Test]
  public void GivenAStandardMetric_WhenICheckThePeriod_ThenTheSameMetricIsReturn()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );
    // When

    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);
    // var result = _timeMathService.AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod(info);
    var ex = Check.ThatCode(
      () =>
        TimeMathService.AssumeWindowRetentionIsValid(info)
      //_timeMathService.AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod(metric)
    ).DoesNotThrow();

    // Then
    Check.That(ex).IsNotNull();
  }

  [Test]
  public void GivenAWindowedMetrics_WhenIGiveATimeSpanLessThanPublicationPeriod_ThenIGetAnException()
  {
    // Given
    var windowPolicy = new WindowPolicy(
      2,
      TimeUnit.Minutes
    );

    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes,
      null,
      null,
      null,
      windowPolicy
    );

    // When
    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);
    var ex = Check.ThatCode(
        () =>
          TimeMathService.AssumeWindowRetentionIsValid(info)
        //_timeMathService.AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod(metric)
      )
      .Throws<InvalidOperationException>().Value;

    // Then
    Check.That(ex.Message).Contains("Window period of Metric must be greater than primary publication period");
  }

  [Test]
  public void GivenAWindowedMetrics_WhenIGiveATimeSpanGreaterThanPublicationPeriod_ThenIGetMetricsEvent()
  {
    // Given
    var windowPolicy = new WindowPolicy(
      6,
      TimeUnit.Minutes
    );

    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes,
      null,
      null,
      null,
      windowPolicy
    );
    // When
    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);
    var ex = Check.ThatCode(
      () =>
        TimeMathService.AssumeWindowRetentionIsValid(info)
      //_timeMathService.AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod(metric)
    ).DoesNotThrow();
    // Then
    Check.That(ex).IsNotNull();
  }

  [Test]
  public void GivenAWindowedMetrics_WhenIGiveATimeSpanNotAMultiplierOfThePublicationPeriod_ThenIGetAnException()
  {
    // Given
    var windowPolicy = new WindowPolicy(
      5,
      TimeUnit.Minutes
    );

    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes,
      null,
      null,
      null,
      windowPolicy
    );

    // When
    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);
    var ex = Check.ThatCode(
        () =>
          //_timeMathService.AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod(metric)
          TimeMathService.AssumeWindowRetentionIsValid(info)
      )
      .Throws<InvalidOperationException>().Value;

    // Then
    Check.That(ex.Message).Contains("Window period of Metric must be a multiplier of the primary publication period");
  }

  [Test]
  public void HandleSystemTicksWithoutMetricsShouldSendEmptyEvents()
  {
    // Given
    // When
    var events = _timeMathService.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        MsInSeconds,
        60 * MsInSeconds
      )
    );

    // Then
    Check.That(events).IsNotNull();
    Check.That(events).HasSize(0);
  }

  [Test]
  public void HandlePropertiesChangedShouldSendEmptyEvents()
  {
    // Given
    var at = TimeSpan.Zero;
    const float temperature = 20;
    // When
    var events = _timeMathService.HandlePropertiesChanged(
      PropertiesChanged.Create(
        new[]
        {
          CreateProperty(
            InputUrn,
            temperature,
            at
          )
        },
        at
      )
    );

    // Then
    Check.That(events).IsNotNull();
    Check.That(events).HasSize(0);
  }

  [Test]
  public void HandleSystemTicksWithoutTickShouldSendThrowAnException()
  {
    // Given
    var at = TimeSpan.Zero;
    float temperature = 1;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      1,
      0
    );
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      2,
      0
    );
    temperature = 2;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    // When
    var ex = Check.ThatCode(
        () =>
          _timeMathService.HandleSystemTicked(null)
      )
      .Throws<ArgumentNullException>().Value;

    // Then
    Check.That(ex.Message).Contains("trigger");
  }

  [Test]
  public void HandleSystemTicksWithoutMetricShouldSendEmptyEvents()
  {
    // Given
    var at = TimeSpan.Zero;
    float temperature = 1;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      1,
      0
    );
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      2,
      0
    );
    temperature = 2;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      2,
      59
    );
    // When
    var events = _timeMathService.HandleSystemTicked(
      SystemTicked.Create(
        at,
        MsInSeconds,
        60 * MsInSeconds
      )
    );

    // Then
    Check.That(events).IsNotNull();
    Check.That(events).HasSize(0);
  }

  [Test]
  public void HandleSystemTicksWithMetricAndTicksLessThanIntervalShouldSendEmptyEvents()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );

    _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      _writer,
      _reader
    );

    var at = TimeSpan.Zero;
    float temperature = 1;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      1,
      0
    );
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      2,
      0
    );
    temperature = 2;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    // When
    var events = _timeMathService.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        MsInSeconds,
        60
      )
    );

    // Then
    Check.That(events).IsNotNull();
    Check.That(events).HasSize(0);
  }

  [Test]
  public void HandleSystemTicksWithMetricsShouldSendOneEvent()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );

    _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      _writer,
      _reader
    );

    var at = TimeSpan.Zero;
    float temperature = 1;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      1,
      0
    );
    SendTemperatureAtMinute(
      temperature,
      at
    );
    at = new TimeSpan(
      0,
      2,
      0
    );
    temperature = 2;
    SendTemperatureAtMinute(
      temperature,
      at
    );
    // When
    var events = _timeMathService.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        MsInMinutes,
        3
      )
    );

    // Then
    Check.That(events).IsNotNull();
    Check.That(events.Length).As("nb values").Equals(1);
  }

  [Test]
  public void GivenAStandardMetric_WhenInitializeTheTimeMathService_ThenThereIsOneEntryAtThePublicationInterval()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );

    // When
    _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      _writer,
      _reader
    );

    var result = _timeMathService.ComputersByPublicationIntervals(_3Minutes);
    // Then
    Check.That(result).IsNotNull();
    Check.That(result).HasSize(1);
    Check.That(result[0]).IsNotNull();
  }

  [Test]
  public void
    GivenAStandardMetric_WhenInitializeTheTimeMathService_ThenTheTimeSeriesDatabaseIsConfigureWithThePublicationInterval()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );
    var timeMathWriter = Mock.Of<ITimeMathWriter>();
    var writerMock = Mock.Get(timeMathWriter);

    // When
    _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      timeMathWriter,
      _reader
    );

    // Then

    string[] suffixes =
      {
        ""
      }
      ;
    writerMock.Verify(
      writer =>
        writer.SetupTimeSeries(
          TemperatureMeasure,
          suffixes,
          T._0
        )
      , Times.Exactly(1)
    );
  }

  [Test]
  public void
    GivenAWindowedMetric_WhenInitializeTheTimeMathService_ThenTheTimeSeriesDatabaseIsConfigureWithTheWindowInterval()
  {
    // Given
    var windowPolicy = new WindowPolicy(
      6,
      TimeUnit.Minutes
    );
    var timeMathWriter = Mock.Of<ITimeMathWriter>();
    var writerMock = Mock.Get(timeMathWriter);

    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes,
      null,
      null,
      null,
      windowPolicy
    );

    // When
    _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      timeMathWriter,
      _reader
    );

    // Then
    string[] suffixes =
      {
        MetricUrn.BuildAccumulatedValue(),
        MetricUrn.BuildSamplesCount()
      }
      ;
    writerMock.Verify(
      writer =>
        writer.SetupTimeSeries(
          TemperatureMeasure,
          suffixes,
          T._6
        )
      , Times.Exactly(1)
    );
  }

  [Test]
  public void
    GivenAStandardMetric_WhenPublishingMetrics_ThenTheLastValueShouldBeSaveToTheHistoricTimeLine()
  {
    // Given
    var startAt = T._3;
    var publishAt = T._3;

    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );

    var writerMock = Mock.Of<ITimeMathWriter>();
    var readerMock = new Mock<ITimeMathReader>
    {
      DefaultValueProvider = new OptionDefaultValueProvider()
    };
    ReaderReturnLastPublishAt(
      readerMock,
      TemperatureMeasure,
      T._0
    );
    ReaderReturnEndAt(
      readerMock,
      TemperatureMeasure,
      publishAt
    );
    var timeMathReader = readerMock.Object;
    var result = _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      writerMock,
      timeMathReader
    );

    // _timeMathReader.ReadLastPublishedAt
    ReaderReturnLastPublishAt(
      readerMock,
      TemperatureMeasure,
      T._0
    );
    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        startAt,
        242
      )
      ;

    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;
    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;

    // When
    var events = _timeMathService.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        MsInMinutes,
        3
      )
    );

    // Then
    Mock.Get(writerMock).Verify(
      writer => writer.AddValueAtPublish(
        TemperatureMeasure,
        "",
        publishAt,
        242
      )
      , Times.Once
    );

    Check.That(result).IsNotNull();
    Check.That(result).HasSize(0);
  }

  [Test]
  public void
    GivenAStandardMetric_WhenRestartingTheTimeMathServiceWithExistingMeasureWithinThePublicationInterval_ThenThereIsNoEvents()
  {
    // Given
    var publishAt = T._3;
    var endAt = T._4;
    var restartAt = T._5;

    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );

    var writerMock = Mock.Of<ITimeMathWriter>();
    var readerMock = new Mock<ITimeMathReader>
    {
      DefaultValueProvider = new OptionDefaultValueProvider()
    };

    var timeMathReader = readerMock.Object;

    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        endAt,
        242
      )
      ;

    ReaderReturnFirstValueAtPublish(
        readerMock,
        TemperatureMeasure,
        "",
        publishAt,
        42
      )
      ;

    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        endAt
      )
      ;

    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        publishAt
      )
      ;
    ReaderReturnLastPublishAt(
      readerMock,
      TemperatureMeasure,
      publishAt
    );

    // When
    _timeMathService = new TimeMathService(() => restartAt);

    var result = _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      writerMock,
      timeMathReader
    );

    // Then
    Check.That(result).IsNotNull();
    Check.That(result).HasSize(0);
  }

  [Test]
  public void
    GivenAStandardMetric_WhenRestartingTheTimeMathServiceWithExistingMeasureAfterThePublicationInterval_ThenThereIsOneEntryEvenThoughNotAtThePublicationInterval()
  {
    // Given
    var startAt = T._3;
    var publishAt = T._3;
    var lastUpdateAt = T._5;
    var restartAt = T._7;

    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasureUrn,
      TemperatureMeasure,
      TemperatureInputUrn,
      _3Minutes
    );

    var writerMock = Mock.Of<ITimeMathWriter>();
    var readerMock = new Mock<ITimeMathReader>
    {
      DefaultValueProvider = new OptionDefaultValueProvider()
    };

    var timeMathReader = readerMock.Object;

    ReaderReturnLastUpdateAt(
        readerMock,
        TemperatureMeasure,
        "",
        lastUpdateAt,
        242
      )
      ;

    ReaderReturnEndAt(
        readerMock,
        TemperatureMeasure,
        publishAt
      )
      ;

    ReaderReturnStartAt(
        readerMock,
        TemperatureMeasure,
        startAt
      )
      ;
    ReaderReturnLastPublishAt(
      readerMock,
      TemperatureMeasure,
      publishAt
    );

    // When
    _timeMathService = new TimeMathService(() => restartAt);

    var result = _timeMathService.Initialize(
      CreateMetricInfos.Execute(new[] { metric },_noStateMachines),
      writerMock,
      timeMathReader
    );

    // Then
    Check.That(result).IsNotNull();
    Check.That(result).HasSize(1);
    Check.That(result[0]).IsNotNull();
    var propertiesChanged = result[0].As<PropertiesChanged>();
    Check.That(propertiesChanged.At).Equals(restartAt);
    Check.That(propertiesChanged.ModelValues).IsNotNull();
    Check.That(propertiesChanged.ModelValues.IsEmpty()).IsFalse();
    Check.That(propertiesChanged.ModelValues.First()).IsNotNull();
    var domainEvent = propertiesChanged.ModelValues.First();
    Check.That(domainEvent.Urn).IsNotNull();
    Check.That(domainEvent.Urn).Equals(TemperatureMeasureUrn);
    Check.That(domainEvent.ToFloat().Value).Equals(242);
  }

  private void SendTemperatureAtMinute(
    float temperature,
    TimeSpan at
  )
  {
    _timeMathService.HandlePropertiesChanged(
      PropertiesChanged.Create(
        new[]
        {
          CreateProperty(
            TemperatureInputUrn,
            temperature,
            at
          )
        },
        at
      )
    );
  }

  private static IDataModelValue CreateProperty(
    string urn,
    float value,
    TimeSpan at
  )
  {
    return Property<FloatValue>.Create(
      PropertyUrn<FloatValue>.Build(urn),
      new FloatValue(value),
      at
    );
  }
  
  private readonly ISubSystemDefinition[] _noStateMachines = Array.Empty<ISubSystemDefinition>();
}
