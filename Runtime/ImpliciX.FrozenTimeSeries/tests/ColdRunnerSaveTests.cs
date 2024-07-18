using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using static ImpliciX.Language.Model.MetricUrn;
using static ImpliciX.Language.Model.Property<ImpliciX.Language.Model.MetricValue>;
using Dsl = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.FrozenTimeSeries.Tests;

public class ColdRunnerSaveTests
{
    [Test]
    public void it_should_store_series_for_simple_metrics()
    {
        var now = TimeSpan.FromMinutes(2);
        var startSampling = TimeSpan.FromMinutes(1);
        var samplesCountUrn = BuildSamplesCount(fake_analytics.sample_metric);
        var accumulatedValueUrn = BuildAccumulatedValue(fake_analytics.sample_metric);

        var pc = PropertiesChanged.Create(fake_analytics.sample_metric, new IDataModelValue[]
        {
            Create(samplesCountUrn, new MetricValue(2f, startSampling, now), now),
            Create(accumulatedValueUrn, new MetricValue(10f, startSampling, now), now)
        }, now);

        var urns = DefinedMetrics.OfType<IMetric>().Select(m => m.TargetUrn).ToArray();
        using var coldMetricDb = ColdMetricsDb.LoadOrCreate(urns, StorageFolderPath);
        var sut = new ColdRunner(urns, coldMetricDb, 0);
        sut.StoreSeries(pc);
        sut.Dispose();
        var storedSeries = ReloadSeries(fake_analytics.sample_metric);
        Check.That(storedSeries).HasSize(1);
        var data = storedSeries[0];
        Check.That(data.At).IsEqualTo(now);
        Check.That(data.Values).ContainsExactly(
            new (samplesCountUrn, 2f),
            new (accumulatedValueUrn, 10f));
    }

    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(StorageFolderPath))
            Directory.Delete(StorageFolderPath, true);

        Directory.CreateDirectory(StorageFolderPath);
    }

    private static MetricsDataPoint[] ReloadSeries(MetricUrn metric)
    {
        return Directory.EnumerateFiles(StorageFolderPath)
            .Select(ColdMetricsDb.LoadCollection)
            .First(file => file.MetaData.Urn == metric)
            .DataPoints.ToArray();
    }

    private static readonly string StorageFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "cold_store");
    private static readonly Metric<MetricUrn>[] DefinedMetrics = new IMetricDefinition[]
    {
        Dsl.Metric(fake_analytics.sample_metric).Is.Minutely.AccumulatorOf(fake_model.fake_index),
        Dsl.Metric(fake_analytics.heating).Is.Hourly
            .StateMonitoringOf(fake_model.public_state)
            .Including("fizz").As.AccumulatorOf(fake_model.supply_temperature.measure)
    }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray();
}