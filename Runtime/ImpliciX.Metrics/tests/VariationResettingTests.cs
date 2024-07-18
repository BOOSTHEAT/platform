using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Tests.Helpers;
using ImpliciX.SharedKernel.Storage;
using NFluent;
using NUnit.Framework;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class VariationResettingTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;

    [SetUp]
    public void SetUp()
    {
        var dbPath = "/tmp/variation_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
        
        _sut = new MetricsService(TimeSpan.FromMinutes(1), () => TimeSpan.Zero);
        _sut.Initialize(metric_definition, _tsReader, _tsWriter);
        _context = new MetricsServiceTestContext(_sut);
    }

    private MetricsService _sut;
    private MetricsServiceTestContext _context;

    private readonly IMetric[] metric_definition =
    {
        MetricsDSL.Metric(fake_analytics_model.sample_metric)
            .Is
            .Every(3).Minutes
            .VariationOf(fake_model.fake_index)
            .Builder.Build<Metric<MetricUrn>>()
    };


    [Test]
    public void nominal_case()
    {
        // VAR = Variation
        // T   0   1   2   3 
        //     |   |   |   | 
        //    RUN  |   |   | 
        //     |   |  VAR PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.ChangeFakeIndexTo(1f, 1);
        _context.ChangeFakeIndexTo(11f, 2);
        var propertiesReceived = _context.AdvanceTimeTo(3);
        var propertiesExpected =
            StateMonitoringTestHelper.CreateSimpleMetric(new[]
                {(fake_analytics_model.sample_metric.ToString(), 10f, 0, 3)});

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }


    [Test]
    public void the_variation_must_return_to_zero_after_publish()
    {
        // VAR = Variation
        // T   0   1   2   3   4   5   6 
        //     |   |   |   |   |   |   | 
        //    RUN  |   |   |   |   |   | 
        //     |   |  VAR PUB  |   |  PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.ChangeFakeIndexTo(1f, 1);
        _context.ChangeFakeIndexTo(11f, 2);
        _context.AdvanceTimeTo(3);

        var propertiesReceived = _context.AdvanceTimeTo(6);
        var propertiesExpected =
            StateMonitoringTestHelper.CreateSimpleMetric(new[]
                {(fake_analytics_model.sample_metric.ToString(), 0f, 3, 6)});

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }

    [Test]
    public void if_nothing_happens_during_the_first_window_the_following_windows_must_be_calculated_correctly()
    {
        // VAR = Variation
        // T   0   1   2   3   4   5   6 
        //     |   |   |   |   |   |   | 
        //    RUN  |   |   |   |   |   | 
        //     |   |   |  PUB  |  VAR PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        var propertiesReceived = _context.AdvanceTimeTo(3);
        _context.ChangeFakeIndexTo(12f, 4);
        _context.ChangeFakeIndexTo(14f, 5);
        propertiesReceived = _context.AdvanceTimeTo(6);
        var propertiesExpected =
            StateMonitoringTestHelper.CreateSimpleMetric(new[]
                {(fake_analytics_model.sample_metric.ToString(), 2f, 3, 6)});

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }
}