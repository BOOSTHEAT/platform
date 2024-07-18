using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.FrozenTimeSeries.Tests;

public class ColdRunnerFilteringTests
{
  [Test]
  public void WhenPropertyChangedFromMetricKnown_ThenIStoreIt()
  {
    Urn outputUrn = "myOutputUrn";
    var metric = new Metric<MetricUrn>(MetricKind.Gauge, MetricUrn.Build(), outputUrn, "myInputUrn",
      TimeSpan.FromMinutes(2));

    var now = TimeSpan.FromMinutes(2);
    var pc = PropertiesChanged.Create(metric.TargetUrn, new[] { PDH.CreateMetricValueProperty(outputUrn, 2.4f, 0, 2) },
      now);
    var (metricUrn, series) = ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).GetValue();

    Check.That(metricUrn).IsEqualTo(outputUrn);
    Check.That(series).HasSize(1);
    var data = series[0];
    Check.That(data.Urn.ToString()).IsEqualTo(outputUrn);
    var dataValue = ((IFloat)data.ModelValue()).ToFloat();
    Check.That(dataValue).IsEqualTo(2.4f);
    Check.That(data.At).IsEqualTo(now);
  }

  [Test]
  public void GivenAppRebooted_WhenPropertyChangedFromMetricKnown_ThenIStoreIt()
  {
    const string outputUrn = "myOutputUrn";
    var metric = new Metric<MetricUrn>(MetricKind.Gauge, MetricUrn.Build(), outputUrn, "myInputUrn",
      TimeSpan.FromMinutes(3));

    var now = TimeSpan.FromMinutes(4);
    var pc = PropertiesChanged.Create(metric.TargetUrn, new[] { PDH.CreateMetricValueProperty(outputUrn, 2.4f, 0, 2) },
      now);
    var (metricUrn, series) = ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).GetValue();

    Check.That(series).HasSize(1);
    var data = series[0];
    Check.That(data.Urn.ToString()).IsEqualTo(outputUrn);
    var dataValue = ((IFloat)data.ModelValue()).ToFloat();
    Check.That(dataValue).IsEqualTo(2.4f);
    Check.That(data.At).IsEqualTo(TimeSpan.FromMinutes(2));
  }

  [Test]
  public void GivenAppRebooted_WhenPropertyChangedFromWindowedMetricKnown_ThenIStoreItForPrimaryPeriod()
  {
    var outputUrn = MetricUrn.Build("myOutputUrn");
    var metric = MetricsDSL.Metric(outputUrn)
      .Is
      .Every(3).Minutes
      .OnAWindowOf(6).Minutes
      .VariationOf("myInputUrn")
      .Builder.Build<Metric<MetricUrn>>();

    var now = TimeSpan.FromMinutes(4);
    var pc = PropertiesChanged.Create(metric.TargetUrn, new[] { PDH.CreateMetricValueProperty(outputUrn, 2.4f, 0, 2) },
      now);
    var (_, series) = ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).GetValue();

    Check.That(series).HasSize(1);
    var data = series[0];
    Check.That(data.Urn).IsEqualTo(outputUrn);
    var dataValue = ((IFloat)data.ModelValue()).ToFloat();
    Check.That(dataValue).IsEqualTo(2.4f);
    Check.That(data.At).IsEqualTo(TimeSpan.FromMinutes(2));
  }

  [Test]
  public void GivenStateMonitoring_WhenPropertyChangedFromMetricKnown_ThenIStoreIt()
  {
    const string outputUrn = "myOutputUrn";
    var metric = new Metric<MetricUrn>(MetricKind.State, MetricUrn.Build(), outputUrn, "myInputUrn",
      TimeSpan.FromMinutes(2));

    var now = TimeSpan.FromMinutes(2);
    var pc = PropertiesChanged.Create(metric.TargetUrn, new[]
    {
      PDH.CreateStateOccurenceProperty(outputUrn, 2f, 0, 2),
      PDH.CreateStateDurationProperty(outputUrn, 1f, 0, 2)
    }, now);

    var (_, series) = ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).GetValue();

    Check.That(series).HasSize(2);

    var occ = series[0];
    Check.That(occ.Urn.ToString()).IsEqualTo("myOutputUrn:occurrence");
    Check.That(((IFloat)occ.ModelValue()).ToFloat()).IsEqualTo(2f);
    Check.That(occ.At).IsEqualTo(now);

    var duration = series[1];
    Check.That(duration.Urn.ToString()).IsEqualTo("myOutputUrn:duration");
    Check.That(((IFloat)duration.ModelValue()).ToFloat()).IsEqualTo(60f);
    Check.That(duration.At).IsEqualTo(now);
  }

  [Test]
  public void WhenPropertyChangedFromUnknownMetric_ThenIDoNotStoreIt()
  {
    var now = TimeSpan.FromMinutes(1);
    var pc = PropertiesChanged.Create(Urn.BuildUrn("unknown"),
      new[] { PDH.CreateDataModelFloatValue("myOutputUrn", 2.4f, now) }, now);
    Check.That(ColdRunner.FilterPropertiesToMetrics(MakeFilter(Array.Empty<Urn>()), pc).IsNone).IsTrue();
  }

  [Test]
  public void Bug7082()
  {
    var metric_1 = MFactory.CreateGaugeMetric(MetricUrn.Build("myOutputUrn"), "myInputUrn", 3)
      .Builder.Build<Metric<MetricUrn>>();

    var metric_2 = MFactory.CreateGaugeMetric(MetricUrn.Build("myOutputUrn_sub_part"), "myInputUrn", 3)
      .Group.Daily
      .Builder.Build<Metric<MetricUrn>>();
    Assert.That(metric_2.GroupPoliciesUrns.Count, Is.EqualTo(1));

    var now = TimeSpan.FromMinutes(10);
    var groupModelValues = metric_2.GroupPoliciesUrns.Keys
      .Select(urn => PDH.CreateMetricValueProperty(urn, 2.4f, now, now));

    var pc = PropertiesChanged.Create(metric_2.GroupPoliciesUrns.Keys.First(), groupModelValues, now);

    var filterMetrics = ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric_1.TargetUrn, metric_2.TargetUrn), pc);
    Check.That(filterMetrics.IsNone).IsTrue();
  }

  [Test]
  public void GivenGaugeWithGroup_WhenPropertyChangedFromMetricGroupPublished_ThenIDoNotStoreIt()
  {
    var metric = MFactory.CreateGaugeMetric(MetricUrn.Build("myOutputUrn"), "myInputUrn", 3)
      .Group.Daily
      .Builder.Build<Metric<MetricUrn>>();
    Assert.That(metric.GroupPoliciesUrns.Count, Is.EqualTo(1));

    var now = TimeSpan.FromMinutes(10);
    var groupModelValues = metric.GroupPoliciesUrns.Keys
      .Select(urn => PDH.CreateMetricValueProperty(urn, 2.4f, now, now));

    var pc = PropertiesChanged.Create(metric.GroupPoliciesUrns.Keys.First(), groupModelValues, now);

    Check.That(ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).IsNone).IsTrue();
  }

  [Test]
  public void GivenVariationWithGroup_WhenPropertyChangedFromMetricGroupPublished_ThenIDoNotStoreIt()
  {
    var metric = MFactory.CreateVariationMetric(MetricUrn.Build("myOutputUrn"), "myInputUrn", 3)
      .Group.Daily
      .Builder.Build<Metric<MetricUrn>>();
    Assert.That(metric.GroupPoliciesUrns.Count, Is.EqualTo(1));

    var now = TimeSpan.FromMinutes(10);
    var groupModelValues = metric.GroupPoliciesUrns.Keys
      .Select(urn => PDH.CreateMetricValueProperty(urn, 2.4f, now, now));

    var pc = PropertiesChanged.Create(metric.GroupPoliciesUrns.Keys.First(), groupModelValues, now);

    Check.That(ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).IsNone).IsTrue();
  }

  [Test]
  public void GivenAccumulatorWithGroup_WhenPropertyChangedFromMetricGroupPublished_ThenIDoNotStoreIt()
  {
    var metric = MFactory.CreateAccumulatorMetric(MetricUrn.Build("myOutputUrn"), "myInputUrn", 3)
      .Group.Hourly
      .Builder.Build<Metric<MetricUrn>>();
    Assert.That(metric.GroupPoliciesUrns.Count, Is.EqualTo(1));

    var groupModelValues = metric.GroupPoliciesUrns.Keys
      .SelectMany(urn => new[]
      {
        PDH.CreateAccumulatorValue(urn, 250, 0, 10),
        PDH.CreateAccumulatorCount(urn, 5, 0, 10)
      });

    var pc = PropertiesChanged.Create(metric.GroupPoliciesUrns.Keys.First(), groupModelValues,
      TimeSpan.FromMinutes(10));

    Check.That(ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).IsNone).IsTrue();
  }

  [Test]
  public void GivenStateMonitoringWithGroup_WhenPropertyChangedFromMetricGroupPublished_ThenIDoNotStoreIt()
  {
    var outputUrn = MetricUrn.Build("myOutputUrn");
    var metric = MFactory.CreateStateMonitoringOfMetric(outputUrn, fake_model.public_state, 3)
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();
    Assert.That(metric.GroupPoliciesUrns.Count, Is.EqualTo(1));

    var groupModelValues = metric.GroupPoliciesUrns.Keys
      .SelectMany(urn => new[]
      {
        PDH.CreateStateOccurenceProperty(new[] { urn.Value, fake_model.PublicState.Running.ToString() }, 1, 0, 10),
        PDH.CreateStateDurationProperty(new[] { urn.Value, fake_model.PublicState.Running.ToString() }, 3, 0, 10)
      });

    var pc = PropertiesChanged.Create(metric.GroupPoliciesUrns.Keys.First(), groupModelValues,
      TimeSpan.FromMinutes(10));
    Check.That(ColdRunner.FilterPropertiesToMetrics(MakeFilter(metric.TargetUrn), pc).IsNone).IsTrue();
  }

  [Test]
  public void WhenPropertyChangedFromKnownGroupUrn_ThenIStoreAllFloatsAndNotOnlyMetrics()
  {
    Urn rootUrn = "myOutputUrn";
    var before = TimeSpan.FromMinutes(2);
    var now = TimeSpan.FromMinutes(5);
    var pc = PropertiesChanged.Create(
      Urn.BuildUrn(rootUrn),
      new IDataModelValue[]
      {
        MakeProperty(new MetricValue(3.8f, before, now), now, rootUrn, "prop1"),
        MakeProperty(new MetricValue(4.15f, before, now), now, rootUrn, "prop2"),
        MakeProperty(Temperature.FromFloat(5.25f).Value, now, rootUrn, "prop3"),
        MakeProperty(Percentage.FromFloat(0.35f).Value, now, rootUrn, "prop4"),
        MakeProperty("foo", now, rootUrn, "prop5"),
      }, now);
    var (baseUrn, series) =
      ColdRunner.FilterPropertiesToMetrics(MakeFilter(Urn.BuildUrn(rootUrn)), pc).GetValue();

    Check.That(baseUrn).IsEqualTo(rootUrn);
    Check.That(series).IsEqualTo(new IDataModelValue[]
    {
      MakeProperty(new MetricValue(3.8f, before, now), now, rootUrn, "prop1"),
      MakeProperty(new MetricValue(4.15f, before, now), now, rootUrn, "prop2"),
      MakeProperty(new MetricValue(5.25f, now, now), now, rootUrn, "prop3"),
      MakeProperty(new MetricValue(0.35f, now, now), now, rootUrn, "prop4"),
    });
  }


  private HashSet<Urn> MakeFilter(params Urn[] urns) => new(urns);

  private Property<T> MakeProperty<T>(T value, TimeSpan at, params string[] urnParts)
    => Property<T>.Create(PropertyUrn<T>.Build(urnParts), value, at);
}