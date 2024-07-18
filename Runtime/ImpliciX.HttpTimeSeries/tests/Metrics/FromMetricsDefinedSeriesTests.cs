using ImpliciX.Data.Metrics;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;
using NFluent;
using static ImpliciX.Language.Metrics.Metrics;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.Metrics;

public class FromMetricsDefinedSeriesTests
{

    [Test]
    public void MetricsWithoutOverThePastShouldBeNotStored()
    {
        var def = Metric(MUrn("foo:g1")).Is.Minutely.GaugeOf("foo:input");
        
        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def) }, Array.Empty<ISubSystemDefinition>());
        var sut = new FromMetricsDefinedSeries(metricInfoSet);
        
        Check.That(sut.RootUrns).IsEmpty();
        var (groupProps, groupRet) = sut.StorablePropertiesForRoot((Urn)"foo:g1");
        Check.That(groupProps.Count).IsEqualTo(0);
    }
    
    [Test]
    public void KeepOnlyMetricsWithRetentionDuration()
    {
        var def1 = Metric(MUrn("foo:g1")).Is.Minutely.GaugeOf("foo:input");
        var def2 = Metric(MUrn("foo:g2")).Is.Minutely.GaugeOf("foo:input").Over.ThePast(10).Hours;
        
        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def1), BuildMetric(def2) }, Array.Empty<ISubSystemDefinition>());
        var sut = new FromMetricsDefinedSeries(metricInfoSet);
        
        Check.That(sut.RootUrns).ContainsExactly((Urn)"foo:g2");
        
        var (groupProps1, groupRet1) = sut.StorablePropertiesForRoot((Urn)"foo:g1");
        Check.That(groupProps1.Count).IsEqualTo(0);
        
        var (groupProps2, groupRet2) = sut.StorablePropertiesForRoot((Urn)"foo:g2");
        Check.That(groupProps2.Count).IsEqualTo(1);
        Check.That(groupProps2).Contains((Urn)"foo:g2");
        Check.That(groupRet2).IsEqualTo(TimeSpan.FromHours(10));
    }

    [Test]
    public void complex_metrics_with_many_over_the_past()
    {
        var def = Metric(MUrn("foo:g1"))
            .Is.Minutely.GaugeOf("foo:input")
            .Over.ThePast(10).Minutes
            .Group.Hourly.Over.ThePast(10).Hours;
        
        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def) }, Array.Empty<ISubSystemDefinition>());
        var sut = new FromMetricsDefinedSeries(metricInfoSet);
        
        Check.That(sut.RootUrns).ContainsExactly((Urn)"foo:g1:_1Hours", (Urn)"foo:g1");
        var (groupProps, groupRet) = sut.StorablePropertiesForRoot((Urn)"foo:g1:_1Hours");
        Check.That(groupProps.Count).IsEqualTo(1);
        Check.That(groupProps).Contains((Urn)"foo:g1:_1Hours");
        Check.That(groupRet).IsEqualTo(TimeSpan.FromHours(10));
        
        var (baseProps, baseRet) = sut.StorablePropertiesForRoot((Urn)"foo:g1");
        Check.That(baseProps.Count).IsEqualTo(1);
        Check.That(baseProps).Contains((Urn)"foo:g1");
        Check.That(baseRet).IsEqualTo(TimeSpan.FromMinutes(10));
    }
    
    
    [TestCaseSource(nameof(SimpleMetricsCases))]
    public void should_consider_only_metrics_with_over_the_past(FluentStep metric,
        Urn expectedRootUrn,
        Urn[] expectedSeriesUrns,
        TimeSpan expectedRetention)
    {
        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(metric) }, Array.Empty<ISubSystemDefinition>());
        var sut = new FromMetricsDefinedSeries(metricInfoSet);

        Check.That(sut.RootUrns).ContainsExactly(expectedRootUrn);
        var (props, ret) = sut.StorablePropertiesForRoot(expectedRootUrn);
        Check.That(props.Count).IsEqualTo(expectedSeriesUrns.Length);
        Check.That(props).Contains(expectedSeriesUrns);
        Check.That(ret).IsEqualTo(expectedRetention);
    }

    public static object[] SimpleMetricsCases =
    {
        new object[]
        {
            Metric(MUrn("foo:a1"))
                .Is
                .Every(1).Minutes
                .AccumulatorOf("foo:input")
                .Over.ThePast(10).Minutes,
            (Urn)"foo:a1",
            new Urn[] { "foo:a1:accumulated_value", "foo:a1:samples_count" },
            TimeSpan.FromMinutes(10)
        },
        new object[]
        {
            Metric(MUrn("foo:v1"))
                .Is
                .Every(1).Minutes
                .VariationOf("foo:input")
                .Over.ThePast(10).Minutes,
            (Urn)"foo:v1",
            new Urn[] { "foo:v1" },
            TimeSpan.FromMinutes(10)
        },
        new object[]
        {
            Metric(MUrn("foo:g1"))
                .Is
                .Every(1).Minutes
                .GaugeOf("foo:input")
                .Over.ThePast(10).Minutes,
            (Urn)"foo:g1",
            new Urn[] { "foo:g1" },
            TimeSpan.FromMinutes(10)
        },
        new object[]
        {
            Metric(MUrn("foo:s1"))
                .Is
                .Every(1).Minutes
                .StateMonitoringOf(StateUrn("state_input"))
                .Over.ThePast(10).Minutes,
            (Urn)"foo:s1",
            new Urn[]
            {
                "foo:s1:Disabled:occurrence", "foo:s1:Disabled:duration", "foo:s1:Running:occurrence",
                "foo:s1:Running:duration"
            },
            TimeSpan.FromMinutes(10)
        },
        new object[]
        {
            Metric(MUrn("foo:g1")).Is.Every(1)
                .Minutes.GaugeOf("foo:input")
                .Group.Every(5).Minutes
                .Over.ThePast(10).Minutes,
            (Urn)"foo:g1:_5Minutes",
            new Urn[] { "foo:g1:_5Minutes" },
            TimeSpan.FromMinutes(10)
        }

    };



}