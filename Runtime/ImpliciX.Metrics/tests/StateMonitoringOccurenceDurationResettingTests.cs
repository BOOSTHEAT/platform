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
public class StateMonitoringOccurenceDurationResettingTests
{
    private MetricsService _sut;
    private MetricsServiceTestContext _context;
    

    [SetUp]
    public void SetUp()
    {
        var dbPath = "/tmp/state_computer_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _sut = new MetricsService(TimeSpan.FromMinutes(1), () => TimeSpan.Zero);
        _sut.Initialize(metric_definition, db, db);
        _context = new MetricsServiceTestContext(_sut);
    }

    private readonly IMetric[] metric_definition =
    {
        MetricsDSL.Metric(fake_analytics_model.public_state_A)
            .Is
            .Every(3).Minutes
            .StateMonitoringOf(fake_model.public_state)
            .Builder.Build<Metric<MetricUrn>>()
    };

    [Test]
    public void WhenStateIsSameDuringTwoPublication()
    {
        // T   0   1   2   3   4   5   6 
        //     |   |   |   |   |   |   | 
        //    RUN  |   |   |   |   |   | 
        //     |   |   |  PUB  |   |  PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        var propertiesReceived = _context.AdvanceTimeTo(3);
        var propertiesExpected = StateMonitoringTestHelper.CreateStateMetric(new[]
        {
            (fake_model.PublicState.Running, 1, 180, 0, 3),
            (fake_model.PublicState.Disabled, 0, 0, 0, 3)
        });

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);

        propertiesReceived = _context.AdvanceTimeTo(6);
        propertiesExpected = StateMonitoringTestHelper.CreateStateMetric(new[]
        {
            (fake_model.PublicState.Running, 1, 180, 3, 6),
            (fake_model.PublicState.Disabled, 0, 0, 3, 6)
        });

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }

    [Test]
    public void WhenThereAreTwoChangesStateBeforePublication()
    {
        // T   0    1   2   3 
        //     |    |   |   | 
        //    RUN  DIS  |   | 
        //     |    |   |  PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.ChangeStateTo(fake_model.PublicState.Disabled, 1);

        var propertiesReceived = _context.AdvanceTimeTo(3);
        var propertiesExpected = StateMonitoringTestHelper.CreateStateMetric(new[]
        {
            (fake_model.PublicState.Running, 1, 60, 0, 3),
            (fake_model.PublicState.Disabled, 1, 120, 0, 3)
        });

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }

    [Test]
    public void WhenTheStateIsSameAfterPublishAndChangeBeforeTheSecondPublication()
    {
        // T   0   1   2   3   4   5   6 
        //     |   |   |   |   |   |   | 
        //    RUN DIS  |   |  RUN  |   | 
        //     |   |   |  PUB  |   |  PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.ChangeStateTo(fake_model.PublicState.Disabled, 1);
        var _ = _context.AdvanceTimeTo(3);
        _context.ChangeStateTo(fake_model.PublicState.Running, 4);
        var propertiesReceived = _context.AdvanceTimeTo(6);

        var propertiesExpected = StateMonitoringTestHelper.CreateStateMetric(new[]
        {
            (fake_model.PublicState.Running, 1, 120, 3, 6),
            (fake_model.PublicState.Disabled, 1, 60, 3, 6)
        });

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }

    [Test]
    public void WhenTheStateChangesAtTheSameTimeOfPublish()
    {
        // T(min)  0   1   2   3   4   5   6 
        //         |   |   |   |   |   |   | 
        //        RUN  |   |  DIS  |   |   | 
        //         |   |   |  PUB  |   |  PUB

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        var _ = _context.AdvanceTimeTo(3);
        _context.ChangeStateTo(fake_model.PublicState.Disabled, 3);
        var propertiesReceived = _context.AdvanceTimeTo(6);

        var propertiesExpected = StateMonitoringTestHelper.CreateStateMetric(new[]
        {
            (fake_model.PublicState.Running, 1, 0, 3, 6),
            (fake_model.PublicState.Disabled, 1, 180, 3, 6)
        });

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }

    [Test]
    public void WhenPreviousStateDoesNotRaiseInPeriod_ThenItsOccurenceMustBeEqualTo0()
    {
        // T(min)   0   1   2   3   4   5   6   7   8  9
        //          |   |   |   |   |   |   | 
        //         RUN  |   |  DIS  |   |   | 
        //          |   |   |  PUB  |   |  PUB
        // RUN occ:             1           1          0
        // RUN dur:             3           0          0
        // DIS occ:             0           1          1
        // DIS dur:             0           3          3

        _context.ChangeStateTo(fake_model.PublicState.Running, 0);
        _context.AdvanceTimeTo(3);
        _context.ChangeStateTo(fake_model.PublicState.Disabled, 3);
        _context.AdvanceTimeTo(6);

        var propertiesReceived = _context.AdvanceTimeTo(9);
        var propertiesExpected = StateMonitoringTestHelper.CreateStateMetric(new[]
        {
            (fake_model.PublicState.Running, 0, 0, 6, 9),
            (fake_model.PublicState.Disabled, 1, 180, 6, 9)
        });

        Check.That(propertiesReceived).Contains(propertiesExpected);
        Check.That(propertiesReceived).HasSize(propertiesExpected.Length);
    }
}