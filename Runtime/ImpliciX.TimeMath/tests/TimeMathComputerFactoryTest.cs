using System;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using ImpliciX.TimeMath.Computers;
using ImpliciX.TimeMath.Tests.Helpers;
using NFluent;
using NUnit.Framework;
using TimeUnit = ImpliciX.Language.Model.TimeUnit;

namespace ImpliciX.TimeMath.Tests;

public class TimeMathComputerFactoryTest
{
  private const string FakeAnalytics = "fake_analytics";
  private const string Measure = "measure";

  private static readonly MetricUrn Temperature = MetricUrn.Build(
    FakeAnalytics,
    nameof(Temperature)
  );

  private static readonly MetricUrn TemperatureMeasure = MetricUrn.Build(
    FakeAnalytics,
    nameof(Temperature),
    Measure
  );

  private static readonly TimeHelper T = TimeHelper.Minutes();
  private readonly TimeSpan _3Minutes = T._3;
  private readonly Func<TimeSpan> _now = () => T._1;
  private TimeMathComputerFactory _computerFactory;

  [SetUp]
  public void Init()
  {
    var timeMathPersistence = new FakeTimeMathPersistence();
    _computerFactory = new TimeMathComputerFactory(
      timeMathPersistence,
      timeMathPersistence
    );
  }

  [Test]
  public void GivenAStandardMetric_WhenICreate_ThenThereIsOneEntryAtThePublicationInterval()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.Gauge,
      TemperatureMeasure,
      TemperatureMeasure,
      Temperature,
      _3Minutes
    );

    // When
    var info = MetricInfoFactory.CreateGaugeInfo(metric);
    var result = _computerFactory.Create(
      info,
      _now()
    );

    // Then
    Check.That(result).IsNotNull();
    Check.That(result.Length).Equals(1);
    Check.That(result[0].Computer).IsInstanceOf<GaugeComputer>();
  }

  [Test]
  public void GivenNoMetric_WhenICreate_ThenIGetAnException()
  {
    // Given
    IMetricInfo metricInfo = null;
    // When
    var ex = Check.ThatCode(
        () =>
          _computerFactory.Create(
            metricInfo!,
            _now()
          )
      )
      .Throws<ArgumentNullException>().Value;

    // Then
    Check.That(ex.Message).Equals("Value cannot be null. (Parameter 'metricInfo')");
  }

  [Test]
  public void GivenAStandardAccumulationMetric_WhenICreate_ThenThereIsOneEntryAtThePublicationInterval()
  {
    // Given
    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasure,
      TemperatureMeasure,
      Temperature,
      _3Minutes
    );

    // When
    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);
    var result = _computerFactory.Create(
      info,
      _now()
    );

    // Then
    Check.That(result).IsNotNull();
    Check.That(result.Length).Equals(1);
    Check.That(result[0].Computer).IsInstanceOf<AccumulatorComputer>();
  }

  [Test]
  public void GivenAWindowedAccumulatorMetric_WhenICreate_ThenThereIsOneEntryAtThePublicationInterval()
  {
    // Given
    var windowPolicy = new WindowPolicy(
      5,
      TimeUnit.Seconds
    );

    var metric = new Metric<MetricUrn>(
      MetricKind.SampleAccumulator,
      TemperatureMeasure,
      TemperatureMeasure,
      Temperature,
      _3Minutes,
      null,
      null,
      null,
      windowPolicy
    );

    // When
    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);
    var result = _computerFactory.Create(
      info,
      _now()
    );

    // Then
    Check.That(result).IsNotNull();
    Check.That(result.Length).Equals(1);
    Check.That(result[0].Computer).IsInstanceOf<AccumulatorComputer>();
  }
}
