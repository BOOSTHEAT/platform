using System;
using System.Linq;
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
public class StateMonitoringAccumulatorResettingTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;



    [SetUp]
    public void SetUp()
    {
        var dbPath = "/tmp/state_accumumlator_tests";
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
        MetricsDSL.Metric(fake_analytics_model.public_state_A)
            .Is
            .Every(3).Minutes
            .StateMonitoringOf(fake_model.public_state)
            .Including("fake_index_acc").As.AccumulatorOf(fake_model.fake_index)
            .Builder.Build<Metric<MetricUrn>>()
    };


    [Test]
    public void nominal_case()
    {
        // T   0   1   2   3   4   5   6 
        //     |   |   |   |   |   |   | 
        //    RUN  |   |   |   |   |   | 
        //     |  ACC  |  PUB  |   |  PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.ChangeFakeIndexTo(1f, 1);
        _context.ChangeFakeIndexTo(11f, 2);
        var propertiesReceived = _context.AdvanceTimeTo(3);
        var propertiesExpected = StateMonitoringTestHelper
            .CreateStateMetric(new[]
            {
                (fake_model.PublicState.Running, 1, 180, 0, 3),
                (fake_model.PublicState.Disabled, 0, 0, 0, 3)
            })
            .Concat(StateMonitoringTestHelper.CreateStateAccumulatorMetric(new[]
            {
                (fake_model.PublicState.Running, "fake_index_acc", 2f, 12f, 0, 3),
                (fake_model.PublicState.Disabled, "fake_index_acc", 0, 0, 0, 3)
            })).ToArray();

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);

        propertiesReceived = _context.AdvanceTimeTo(6);
        propertiesExpected =
            StateMonitoringTestHelper.CreateStateMetric(new[]
                {
                    (fake_model.PublicState.Running, 1, 180, 3, 6),
                    (fake_model.PublicState.Disabled, 0, 0, 3, 6)
                })
                .Concat(StateMonitoringTestHelper.CreateStateAccumulatorMetric(new[]
                {
                    (fake_model.PublicState.Running, "fake_index_acc", 0f, 0f, 3, 6),
                    (fake_model.PublicState.Disabled, "fake_index_acc", 0f, 0f, 3, 6)
                })).ToArray();

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }
}