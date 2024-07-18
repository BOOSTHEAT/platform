using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Storage;
using Moq;
using static ImpliciX.Language.Metrics.Metrics;
using static ImpliciX.TimeCapsule.Tests.Helpers;
using DateTime = System.DateTime;

namespace ImpliciX.TimeCapsule.Tests;

public class HotRunnerAppliesOverThePastTests
{
  private const string DbFolderPath = "/tmp/hot_runner_over_the_past";


  [SetUp]
  public void Setup()
  {
    if (Directory.Exists(DbFolderPath))
      Directory.Delete(DbFolderPath, true);
  }

  [Test]
  public void bug()
  {
    var db = new TimeSeriesDb(DbFolderPath, "test");
    var clock = Mock.Of<IClock>(it => it.Now() == DateTime.Now.TimeOfDay);
    var hr = new HotRunner(new IMetricDefinition[]
      {
        Metric(fake_analytics.temperature).Is.Every(1).Seconds
          .GaugeOf(fake_model.temperature.Urn).Over.ThePast(2).Years,
      }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray(),
      new (Urn, ChartXTimeSpan)[]
      {
        ("fake_analytics:temperature", new ChartXTimeSpan(2, TimeUnit.Seconds)),
      },
      db, db, clock);
    for (int i = 0; i < 100; i++)
    {
      var pc = PropertiesChanged.Create(
        fake_analytics.temperature,
        new IDataModelValue[]
        {
          new DataModelValue<MetricValue>(fake_analytics.temperature, MV(42f), TimeSpan.FromSeconds(i))
        },
        TimeSpan.FromSeconds(i));

      hr.StoreSeries(pc);
    }

    var allDataPoints = db.ReadAll(fake_analytics.temperature)
      .GetValueOrDefault(Array.Empty<DataModelValue<float>>());
    Assert.That(allDataPoints.Length, Is.EqualTo(3));
  }

  [TestCaseSource(nameof(CanHandleCases))]
  public void can_handle(Urn groupUrn, bool expected)
  {
    var definedMetrics = new IMetricDefinition[]
    {
      Metric(fake_analytics.sample_metric).Is.Minutely.AccumulatorOf(fake_model.fake_index)
        .Group.Daily,

      Metric(fake_analytics.temperature_delta).Is.Minutely.VariationOf(fake_model.temperature.Urn)
        .Over.ThePast(10).Years
        .Group.Hourly.Over.ThePast(10).Years,

      Metric(fake_analytics.temperature).Is.Minutely.GaugeOf(fake_model.temperature.Urn)
        .Over.ThePast(10).Years
        .Group.Every(5).Minutes.Over.ThePast(10).Years,

      Metric(fake_analytics.heating).Is.Minutely.StateMonitoringOf(fake_model.public_state)
        .Including("productivity").As.VariationOf(fake_model.supply_temperature.measure)
        .Over.ThePast(1).Years
        .Group.Hourly.Over.ThePast(1).Years
    }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray();

    var xSpans = new (Urn, ChartXTimeSpan)[]
    {
      ("fake_analytics:temperature_delta", new ChartXTimeSpan(10, TimeUnit.Days)),
      ("fake_analytics:temperature_delta:_1Hours", new ChartXTimeSpan(10, TimeUnit.Days)),
      ("fake_analytics:temperature", new ChartXTimeSpan(10, TimeUnit.Minutes)),
      ("fake_analytics:temperature:_5Minutes", new ChartXTimeSpan(10, TimeUnit.Minutes)),
      ("fake_analytics:heating", new ChartXTimeSpan(1, TimeUnit.Days)),
      ("fake_analytics:heating:_1Hours", new ChartXTimeSpan(1, TimeUnit.Days)),
    };

    var db = new TimeSeriesDb(DbFolderPath, "test");
    var clock = Mock.Of<IClock>(it => it.Now() == DateTime.Now.TimeOfDay);
    var hr = new HotRunner(definedMetrics, xSpans, db, db, clock);
    var propertiesChanged = PropertiesChanged.Create(groupUrn, Array.Empty<IDataModelValue>(), TimeSpan.FromSeconds(0));

    Assert.That(hr.CanHandle(propertiesChanged), Is.EqualTo(expected));
  }

  public static object[] CanHandleCases =
  {
    new object[] { null, false },
    new object[] { (Urn)"not:a:metric", false },
    new object[] { (Urn)"fake_analytics:sample_metric", false }, // don't have over the past option
    new object[] { (Urn)"fake_analytics:sample_metric:_1Days", false }, // don't have over the past option
    new object[] { (Urn)"fake_analytics:temperature_delta", true },
    new object[] { (Urn)"fake_analytics:temperature_delta:_1Hours", true },
    new object[] { (Urn)"fake_analytics:temperature", true },
    new object[] { (Urn)"fake_analytics:temperature:_5Minutes", true },
    new object[] { (Urn)"fake_analytics:heating", true },
    new object[] { (Urn)"fake_analytics:heating:_1Hours", true },
  };
}