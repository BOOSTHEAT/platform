using ImpliciX.Data.Metrics;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.Language.Control;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using static ImpliciX.Language.Metrics.Metrics;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.Metrics;

[TestFixture(typeof(TimeSeriesDbRepository))]
[TestFixture(typeof(ColdMetricsDbRepository))]
[NonParallelizable]
public class ServiceQueryTests<R> where R : IMetricsDbRepository
{
    private readonly long _currentDateTicks = new DateTime(2023, 12, 12).Ticks;
    
    [Test]
    public void test_gauge_metric_query()
    {
        var def = Metric(MUrn("foo:g1"))
            .Is.Minutely.GaugeOf("foo:input")
            .Over.ThePast(10).Minutes;

        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def) }, Array.Empty<ISubSystemDefinition>());
        var sut = CreateSut(metricInfoSet);

        var valuesByUrn = new Dictionary<string, TimeSeriesValue[]>
        {
            ["foo:g1"] = Enumerable.Range(1, 2)
                .Select(o => new TimeSeriesValue(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o), o))
                .ToArray()
        };
        PopulateDb(sut, "foo:g1", valuesByUrn);

        var results = sut.ReadDbSeriesValues("foo:g1").ToArray();
        Check.That(results).HasSize(2);
        Check.That(results).Contains(new TimeSeriesValue[]
        {
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(1), 1),
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(2), 2),
        });
    }

    [Test]
    public void test_variation_metric_query()
    {
        var def = Metric(MUrn("foo:v1"))
            .Is.Minutely.VariationOf("foo:input")
            .Over.ThePast(10).Minutes;

        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def) }, Array.Empty<ISubSystemDefinition>());
        var sut = CreateSut(metricInfoSet);

        var valuesByUrn = new Dictionary<string, TimeSeriesValue[]>
        {
            ["foo:v1"] = Enumerable.Range(1, 2)
                .Select(o => new TimeSeriesValue(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o), o))
                .ToArray()
        };

        PopulateDb(sut, "foo:v1", valuesByUrn);

        var results = sut.ReadDbSeriesValues("foo:v1").ToArray();
        Check.That(results).HasSize(2);
        Check.That(results).Contains(new TimeSeriesValue[]
        {
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(1), 1),
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(2), 2),
        });
    }
    
    [Test]
    public void test_accumulator_metric_query()
    {
        var def = Metric(MUrn("foo:a1"))
            .Is.Minutely.AccumulatorOf("foo:input")
            .Over.ThePast(10).Minutes;

        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def) }, Array.Empty<ISubSystemDefinition>());
        var sut = CreateSut(metricInfoSet);

        var valuesByUrn = new Dictionary<string, TimeSeriesValue[]>
        {
            ["foo:a1:accumulated_value"] = Enumerable.Range(1, 2)
                .Select(o => new TimeSeriesValue(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o), o))
                .ToArray(),
            ["foo:a1:samples_count"] = Enumerable.Range(1, 2)
                .Select(o => new TimeSeriesValue(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o), o*2))
                .ToArray()

        };

        PopulateDb(sut,"foo:a1", valuesByUrn);

        var resultsAcc = sut.ReadDbSeriesValues("foo:a1:accumulated_value").ToArray();
        Check.That(resultsAcc).HasSize(2);
        Check.That(resultsAcc).Contains(new TimeSeriesValue[]
        {
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(1), 1),
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(2), 2),
        });

        var resultsCount = sut.ReadDbSeriesValues("foo:a1:samples_count").ToArray();
        Check.That(resultsCount).HasSize(2);
        Check.That(resultsCount).Contains(new TimeSeriesValue[]
        {
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(1), 2),
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(2), 4),
        });
    }

    [Test]
    public void test_state_monitoring_metric_query()
    {
        var def = Metric(MUrn("foo:s1"))
            .Is
            .Every(1).Minutes
            .StateMonitoringOf(StateUrn("state_input"))
            .Over.ThePast(10).Minutes;
        
        var metricInfoSet = CreateMetricInfos.Execute(new[] { BuildMetric(def) }, Array.Empty<ISubSystemDefinition>());
        var sut = CreateSut(metricInfoSet);

        var valuesByUrn = new Dictionary<string, TimeSeriesValue[]>
        {
            ["foo:s1:Disabled:occurrence"] = Enumerable.Range(1, 2)
                .Select(o => new TimeSeriesValue(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o), o))
                .ToArray(),
            ["foo:s1:Disabled:duration"] = Enumerable.Range(1, 2)
                .Select(o => new TimeSeriesValue(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o), o*2))
                .ToArray()

        };

        PopulateDb(sut,"foo:s1", valuesByUrn);
        
        var resultsAcc = sut.ReadDbSeriesValues("foo:s1:Disabled:occurrence").ToArray();
        Check.That(resultsAcc).HasSize(2);
        Check.That(resultsAcc).Contains(new TimeSeriesValue[]
        {
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(1), 1),
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(2), 2),
        });

        var resultsCount = sut.ReadDbSeriesValues("foo:s1:Disabled:duration").ToArray();
        Check.That(resultsCount).HasSize(2);
        Check.That(resultsCount).Contains(new TimeSeriesValue[]
        {
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(1), 2),
            new(TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(2), 4),
        });
    }

    private DataService CreateSut(MetricInfoSet metricInfoSet)
    {
        return new DataService(new FromMetricsDefinedSeries(metricInfoSet), DbFactory);

        IMetricsDbRepository DbFactory(IDefinedSeries definedSeries)
        {
            var testFolder = Path.Combine(Path.GetTempPath(), $"QueryEndPointTest{typeof(R).Name}");
            if (Directory.Exists(testFolder))
                Directory.Delete(testFolder, true);

            return typeof(R) switch
            {
                var t when t == typeof(TimeSeriesDbRepository) => new TimeSeriesDbRepository(definedSeries, testFolder,
                    "test"),
                var t when t == typeof(ColdMetricsDbRepository) => new ColdMetricsDbRepository(definedSeries,
                    testFolder),
                _ => throw new Exception($"Unknown type {typeof(R).Name}")
            };
        }
    }
}