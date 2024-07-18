using ImpliciX.Data.Metrics;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.HttpTimeSeries.Tests.Helpers;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using Moq;
using static ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.HttpTimeSeries.Tests.Metrics;

public class ServiceCanHandleTests
{
  [TestCaseSource(nameof(CanHandleCases))]
  public void can_handle(Urn groupUrn, bool expected)
  {
    var metrics = new IMetricDefinition[]
    {
      Metric(fake_analytics.sample_metric).Is.Minutely.AccumulatorOf(fake_model.fake_index)
        .Group.Daily,

      Metric(fake_analytics.accumulated).Is.Minutely.AccumulatorOf(fake_model.fake_index)
        .Group.Daily.Over.ThePast(10).Days,

      Metric(fake_analytics.temperature_delta).Is.Minutely.VariationOf(fake_model.temperature.Urn)
        .Over.ThePast(10).Days
        .Group.Hourly.Over.ThePast(10).Days,

      Metric(fake_analytics.temperature).Is.Minutely.GaugeOf(fake_model.temperature.Urn)
        .Over.ThePast(10).Minutes
        .Group.Every(5).Minutes.Over.ThePast(10).Minutes,

      Metric(fake_analytics.heating).Is.Minutely.StateMonitoringOf(fake_model.public_state)
        .Including("productivity").As.VariationOf(fake_model.supply_temperature.measure)
        .Over.ThePast(1).Days
        .Group.Hourly.Over.ThePast(1).Days
    }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray();

    var series = new FromMetricsDefinedSeries(CreateMetricInfos.Execute(metrics, Array.Empty<ISubSystemDefinition>()));
    var sut = new DataService(series, _ => Mock.Of<IMetricsDbRepository>());
    var propertiesChanged = PropertiesChanged.Create(groupUrn, Array.Empty<IDataModelValue>(), TimeSpan.FromSeconds(0));

    Assert.That(sut.CanHandle(propertiesChanged), Is.EqualTo(expected));
  }

  public static object[] CanHandleCases =
  {
    new object[] {null, false},
    new object[] {(Urn) "not:a:metric", false},
    new object[] {(Urn) "fake_analytics:sample_metric", false}, // don't have over the past option
    new object[] {(Urn) "fake_analytics:sample_metric:_1Days", false}, // don't have over the past option
    new object[] {(Urn) "fake_analytics:accumulated", false}, // don't have over the past option
    new object[] {(Urn) "fake_analytics:accumulated:_1Days", true},
    new object[] {(Urn) "fake_analytics:temperature_delta", true},
    new object[] {(Urn) "fake_analytics:temperature_delta:_1Hours", true},
    new object[] {(Urn) "fake_analytics:temperature", true},
    new object[] {(Urn) "fake_analytics:temperature:_5Minutes", true},
    new object[] {(Urn) "fake_analytics:heating", true},
    new object[] {(Urn) "fake_analytics:heating:_1Hours", true},
  };
}