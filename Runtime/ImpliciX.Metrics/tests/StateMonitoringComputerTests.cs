using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.Metrics.Tests.Helpers;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Model.MetricUrn;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class StateMonitoringComputerTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();
    private const string FakeIndex = "fake_index";
    private const string TemperatureAcc = "temperature_acc";
    private const string TemperatureDelta = "temperature_delta";
    private static readonly PropertyUrn<fake_model.PublicState> InputProperty = fake_model.public_state;
    private static readonly MetricUrn OutputUrn = fake_analytics_model.public_state_A2;
    private readonly MetricUrn Running_BaseUrn = Build(OutputUrn, fake_model.PublicState.Running.ToString());
    private readonly MetricUrn Disabled_BaseUrn = Build(OutputUrn, fake_model.PublicState.Disabled.ToString());


    [SetUp]
    public void Init()
    {
        var dbPath = "/tmp/state_computer_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
    }

    #region When I Init

    [Test]
    public void GivenInputEnumStateTypeIsNotAnEnum_WhenIInit_ThenIGetError()
    {
        var ex = Check.ThatCode(() => CreateSut(TestCaseId.StateWithoutIncluding, T._5, enumType: typeof(SubsystemState)))
            .Throws<ArgumentException>()
            .Value;

        Check.That(ex.Message).Contains("Type provided must be an Enum.");
    }

    [Test]
    public void GivenITryToUseMetricKindNotAllowed_WhenIInit_ThenIGetAnException()
    {
        var allowedMetricKinds = new[] {MetricKind.Variation, MetricKind.SampleAccumulator};
        var notAllowedMetricKinds = Enum.GetValues<MetricKind>().Except(allowedMetricKinds);

        foreach (var metricKind in notAllowedMetricKinds)
        {
            var ex = Check.ThatCode(() =>
                {
                    var measureRuntimeInfos = new[] {new MeasureRuntimeInfo(new SubMetricDef(FakeIndex, metricKind, fake_model.fake_index))};
                    return new StateMonitoringComputer(OutputUrn, typeof(fake_model.PublicState),
                        TimeSpan.FromDays(1), null, measureRuntimeInfos, _tsReader, _tsWriter, TimeSpan.Zero);
                })
                .Throws<InvalidOperationException>()
                .Value;

            Check.That(ex.Message).Contains($"metric kind is unknown for {nameof(StateMonitoringMeasureKind)}");
        }
    }

    #endregion

    [Test]
    public void GivenValueAndStateChangedDuringPeriod_WhenIPublish_ThenIGetValuesExpected()
    {
        var sut = CreateSut(TestCaseId.StateWithVariationAndAccumulator, T._10);
        //          Temps(min): 0   1   2   3   4 5 6 7  8 9  10
        //               State: RUN         DIS
        //           FakeIndex: 42      30  10
        //         Temperature:     80  84            73
        //PUB Var FakeIndex---
        //       For RUN state:                              -12 (30-42)
        //       For DIS state:                              -20 (10-30)  
        //PUB Acc Temperature--
        //   Acc for RUN state:                               164 (80+84) 
        // Count for RUN state:                               2
        //   Acc for DIS state:                               73
        // Count for DIS state:                               1

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 0));
        sut.Update(CreateFakeIndexProperty(42f, 0));
        sut.Update(CreateTemperatureProperty(80f, 1));
        sut.Update(CreateFakeIndexProperty(30f, 2));
        sut.Update(CreateTemperatureProperty(84f, 2));

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Disabled, 3));
        sut.Update(CreateFakeIndexProperty(10f, 3));
        sut.Update(CreateTemperatureProperty(73f, 7));

        sut.Update(T._10);
        var publish = sut.Publish(T._10);

        // Then
        var expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 10),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 0, 10),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 10),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 7, 0, 10),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(-12f, T._0, T._10), T._10),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(-20f, T._0, T._10), T._10),

            PDH.CreateAccumulatorValue(Build(Running_BaseUrn.Value, TemperatureAcc), 164f, 0, 10),
            PDH.CreateAccumulatorCount(Build(Running_BaseUrn.Value, TemperatureAcc), 2f, 0, 10),
            PDH.CreateAccumulatorValue(Build(Disabled_BaseUrn.Value, TemperatureAcc), 73f, 0, 10),
            PDH.CreateAccumulatorCount(Build(Disabled_BaseUrn.Value, TemperatureAcc), 1f, 0, 10)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);
    }

    #region When variation metric included

    [Test]
    public void GivenVariation_OneValueOnRunningAndOneValueOnDisabled_WhenIPublish_ThenIComputeVariationWithPreviousValueKnown()
    {
        var sut = CreateSut(TestCaseId.StateWithTwoVariations, T._10);

        //      Temps(min): 0   1   2   3   4  10
        //           State: RUN         DIS
        //       FakeIndex:     5           8
        //PUB FakeIndex---
        //   For RUN state:                    No publish because not enough values
        //   For DIS state:                    3 (8-5)  

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 0));
        sut.Update(CreateFakeIndexProperty(5f, 1));

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Disabled, 3));
        sut.Update(CreateFakeIndexProperty(8f, 4));

        var publishAt = TimeSpan.FromMinutes(10);
        sut.Update(publishAt);
        var publish = sut.Publish(publishAt);

        // Then
        var startAt = TimeSpan.Zero;
        var expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 10),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 0, 10),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 10),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 7, 0, 10),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(3f, startAt, publishAt), publishAt),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(0f, startAt, publishAt), publishAt)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);
    }

    [Test]
    public void GivenVariation_NominalCase_WhenIPublish()
    {
        var sut = CreateSut(TestCaseId.StateWithOneVariation, T._10);

        //         Publish:                                          |
        //      Temps(min): 0   1   2   3   4   5   6   7   8   9    10  
        //           State: RUN         DIS         RUN         DIS
        //       FakeIndex:     5  10   4   8       11  13      17   21
        //PUB FakeIndex---
        //   For RUN state:                                          10 = 5+5 (10-5 + 13-8)
        //   For DIS state:                                           2 = -2+4 (8-10 + 17-13)
        //          Global:                                          12(17-5)
        //             RUN: ----3min----|           |---3min----|      = 6min
        //             DIS:             |----3min---|           |1min| = 4min

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 0));
        sut.Update(CreateFakeIndexProperty(5f, 1));
        sut.Update(CreateFakeIndexProperty(10f, 2));

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Disabled, 3));
        sut.Update(CreateFakeIndexProperty(4f, 3));
        sut.Update(CreateFakeIndexProperty(8f, 4));

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 6));
        sut.Update(CreateFakeIndexProperty(11f, 6));
        sut.Update(CreateFakeIndexProperty(13f, 7));

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Disabled, 9));
        sut.Update(CreateFakeIndexProperty(17f, 9));
        sut.Update(CreateFakeIndexProperty(21f, 10));

        sut.Update(T._10);
        var publish = sut.Publish(T._10);

        // Then
        var expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2, 0, 10),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 6, 0, 10),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 2, 0, 10),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 4, 0, 10),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(10f, T._0, T._10), T._10),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(2f, T._0, T._10), T._10)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);
    }

    [Test]
    public void GivenWindowBiggerThanMetricPeriod()
    {
        var sut = CreateSut(TestCaseId.StateWithOneVariation, publicationPeriod: T._4, windowPeriod: T._8);

        //         Publish:                 |               |                 |                   |
        //      Temps(min): 0   1   2   3   4   5   6   7   8   9   10   11   12   13   14   15   16
        //           State: RUN         DIS         RUN         DIS                     RUN 
        //       FakeIndex:     5  10   4   8       11  13      17  21   7    9    22        3
        //PUB -------------
        //VAR on RUN state:                 5(10-5)         10(10-5 + 13-8)   5(13-8)            -19(3-22)
        //VAR on DIS state:                -6(4-10)        -2(8-10)          -2(8-4 + 7-13)        9(22-13)
        //          Global:                -1(4-5)          8(13-5)           3(7-4)             -10(3-13)
        //             RUN: ----3min----|           |---3min----|                       |---2min--|     
        //             DIS:             |----3min---|           |----------5min---------|              
        //   RUN occurence:                 1               2                  1                  2
        //   RUN dura(min):                 3               5(3-0 + 8-6)       3(9-6)             3(9-8 + 16-14)
        //   DIS occurence:                 1               1                  2                  1
        //   DIS dura(min):                 1               3(6-3)             5(6-4 + 12-9)      5(14-9)

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 0));
        sut.Update(CreateFakeIndexProperty(5f, 1));
        sut.Update(CreateFakeIndexProperty(10f, 2));

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Disabled, 3));
        sut.Update(CreateFakeIndexProperty(4f, 3));
        sut.Update(CreateFakeIndexProperty(8f, 4));

        sut.Update(T._4);
        var publish = sut.Publish(T._4);

        var expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 4),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 0, 4),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 4),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 1, 0, 4),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(5f, T._0, T._4), T._4),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(-6f, T._0, T._4), T._4)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 6));
        sut.Update(CreateFakeIndexProperty(11f, 6));
        sut.Update(CreateFakeIndexProperty(13f, 7));

        sut.Update(T._8);
        publish = sut.Publish(T._8);

        expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2, 0, 8),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 5, 0, 8),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 8),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 3, 0, 8),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(10f, T._0, T._8), T._8),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(-2f, T._0, T._8), T._8)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);

        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Disabled, 9));
        sut.Update(CreateFakeIndexProperty(17f, 9));
        sut.Update(CreateFakeIndexProperty(21f, 10));
        sut.Update(CreateFakeIndexProperty(7f, 11));
        sut.Update(CreateFakeIndexProperty(9f, 12));

        sut.Update(T._12);
        publish = sut.Publish(T._12);

        expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 4, 12),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 4, 12),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 2, 4, 12),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 5, 4, 12),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(5f, T._4, T._12), T._12),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(-2f, T._4, T._12), T._12)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);


        sut.Update(CreateFakeIndexProperty(22f, 13));
        sut.Update(CreatePublicStateProperty(fake_model.PublicState.Running, 14));
        sut.Update(CreateFakeIndexProperty(3f, 15));

        sut.Update(T._16);
        publish = sut.Publish(T._16);

        expected = new IDataModelValue[]
        {
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2, 8, 16),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 8, 16),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 8, 16),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 5, 8, 16),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(-19f, T._8, T._16), T._16),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(9f, T._8, T._16), T._16)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);
    }

    #endregion

    #region When accumulator metric included

    [Test]
    public void GivenAccumulatorIncluded_WhenIPublish_ThenIGetPropertiesToPublishExpected()
    {
        var sut = CreateSut(TestCaseId.StateWithAccumulator, TimeSpan.FromMinutes(5));

        // Publish                        |                   |
        //    T(min): 0   1   2   3   4   5   6   7   8   9   10
        //     State: RUN
        //     Value:    100 160 80          10  60  -30 -117
        // Acc Value:                    340                 -77
        // Acc Count:                     3                   4

        // When i update and publish the sequence 1
        sut.Update(CreateRunningProperty(0));
        sut.Update(CreateTemperatureProperty(100f, 1));
        sut.Update(CreateTemperatureProperty(160f, 2));
        sut.Update(CreateTemperatureProperty(80f, 3));
        sut.Update(T._5);
        var publish = sut.Publish(T._5);

        // Then i get sequence 1 result expected
        IDataModelValue[] expected =
        {
            PDH.CreateAccumulatorCount(Build(Disabled_BaseUrn, TemperatureAcc), 0f, 0, 5),
            PDH.CreateAccumulatorValue(Build(Disabled_BaseUrn, TemperatureAcc), 0f, 0, 5),
            PDH.CreateAccumulatorCount(Build(Running_BaseUrn, TemperatureAcc), 3f, 0, 5),
            PDH.CreateAccumulatorValue(Build(Running_BaseUrn, TemperatureAcc), 340f, 0, 5),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 5),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 5, 0, 5),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 5),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 5)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);

        sut.Update(CreateTemperatureProperty(10f, 6));
        sut.Update(CreateTemperatureProperty(60f, 7));
        sut.Update(CreateTemperatureProperty(-30f, 8));
        sut.Update(CreateTemperatureProperty(-117f, 9));
        sut.Update(T._10);
        publish = sut.Publish(T._10);

        // When i update and publish sequence 2 without create a new StateMonitoringComputer

        expected = new IDataModelValue[]
        {
            PDH.CreateAccumulatorCount(Build(Disabled_BaseUrn, TemperatureAcc), 0f, 5, 10),
            PDH.CreateAccumulatorValue(Build(Disabled_BaseUrn, TemperatureAcc), 0f, 5, 10),
            PDH.CreateAccumulatorCount(Build(Running_BaseUrn, TemperatureAcc), 4f, 5, 10),
            PDH.CreateAccumulatorValue(Build(Running_BaseUrn, TemperatureAcc), -77f, 5, 10),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 5, 10),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 5, 5, 10),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 5, 10),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 5, 10)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);
    }

    #endregion

    #region When variation and accumulator metrics included

    [Test]
    public void GivenVariationAndAccumulatorIncluded_WhenIPublish_ThenIGetPropertiesToPublishExpected()
    {
        var sut = CreateSut(TestCaseId.StateWithVariationAndAccumulator, TimeSpan.FromMinutes(5));

        //          Publish:                     |   
        //           T(min): 0   1   2   3   4   5   
        //            State: RUN
        //           Tempe.:    100 160  80          
        //        FakeIndex:     10  60 -30 -17
        // Tempe. Acc Value:                    340                 
        // Temps. Acc Count:                     3                  
        //   Var fakeIndex:                     -27 

        // When i update and publish variation and accumulator sequences
        sut.Update(CreateRunningProperty(0));
        sut.Update(CreateFakeIndexProperty(10f, 1));
        sut.Update(CreateTemperatureProperty(100f, 1));
        sut.Update(CreateFakeIndexProperty(60f, 2));
        sut.Update(CreateTemperatureProperty(160f, 2));
        sut.Update(CreateFakeIndexProperty(-30f, 3));
        sut.Update(CreateTemperatureProperty(80f, 3));
        sut.Update(CreateFakeIndexProperty(-17f, 4));
        sut.Update(T._5);
        var publish = sut.Publish(T._5);

        IDataModelValue[] expected =
        {
            PDH.CreateAccumulatorCount(Build(Disabled_BaseUrn, TemperatureAcc), 0f, 0, 5),
            PDH.CreateAccumulatorValue(Build(Disabled_BaseUrn, TemperatureAcc), 0f, 0, 5),
            PDH.CreateAccumulatorCount(Build(Running_BaseUrn, TemperatureAcc), 3f, 0, 5),
            PDH.CreateAccumulatorValue(Build(Running_BaseUrn, TemperatureAcc), 340f, 0, 5),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 5),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 5, 0, 5),
            Property<MetricValue>.Create(Build(Disabled_BaseUrn, FakeIndex), new MetricValue(0f, T._0, T._5), T._5),
            Property<MetricValue>.Create(Build(Running_BaseUrn, FakeIndex), new MetricValue(-27f, T._0, T._5), T._5),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 5),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 5)
        };

        Check.That(publish.GetValue()).Contains(expected);
        Check.That(publish.GetValue()).HasSize(expected.Length);
    }

    #endregion

    [Test]
    public void GivenNoStateKnown_WhenIPublish_ThenEventsToPublishIsNone()
    {
        var sut = CreateSut(TestCaseId.StateWithTwoVariations, TimeSpan.FromMinutes(25));
        var publish = sut.Publish(TimeSpan.FromMinutes(25));
        Check.That(publish.IsNone).IsTrue();
    }

    [Test]
    public void GivenStateIsKnownButDontChangeDuringNextPeriods_WhenIPublish_ThenOccurenceIsEqualsTo1()
    {
        var publicationPeriod = TimeSpan.FromMinutes(50);
        var sut = CreateSut(TestCaseId.StateWithTwoVariations, publicationPeriod);

        //    Temps(min): 0   10   50   100
        //         State:     RUN
        // Pub Occurence:          1    1
        // Pub  Duration:          40   50

        sut.Update(CreateRunningProperty(10));
        sut.Update(publicationPeriod);
        var publishResults = sut.Publish(publicationPeriod);

        IDataModelValue[] expected =
        {
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 50),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 50 - 10, 0, 50),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 50),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1f, 0, 50)
        };

        Check.That(publishResults.GetValue()).Contains(expected);
        Check.That(publishResults.GetValue()).HasSize(expected.Length);

        var now = publicationPeriod * 2;
        sut.Update(now);
        publishResults = sut.Publish(now);

        Check.That(publishResults.IsSome).IsTrue();

        expected = new IDataModelValue[]
        {
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 50, 100),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 50, 50, 100),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 50, 100),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 50, 100)
        };

        Check.That(publishResults.GetValue()).Contains(expected);
        Check.That(publishResults.GetValue()).HasSize(expected.Length);
    }

    [Test]
    public void GivenStateChangedBetweenEachPublish_WhenIPublish_ThenIGetOccurenceAndDurationExpected()
    {
        var publicationPeriod = TimeSpan.FromMinutes(60);
        var sut = CreateSut(TestCaseId.StateWithTwoVariations, publicationPeriod);

        //  Time (min) : 0  10   20   30   40   50   60   70   80   105  120
        //       State :    Run  Dis  Run             |        Dis  Run   |
        //Publish-------                             PUB                 PUB
        //Run occurence:                             2                   2
        // Run duration:                             40                  35
        //Dis occurence:                             1                   1
        // Dis duration:                             10                  25

        sut.Update(CreateRunningProperty(10));
        sut.Update(CreateDisabledProperty(20));
        sut.Update(CreateRunningProperty(30));

        sut.Update(publicationPeriod);
        var publishResult = sut.Publish(publicationPeriod);

        Check.That(publishResult.IsSome).IsTrue();

        IDataModelValue[] expected =
        {
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 10f, 0, 60),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 40f, 0, 60),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1f, 0, 60),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2f, 0, 60)
        };

        Check.That(publishResult.GetValue()).Contains(expected);
        Check.That(publishResult.GetValue()).HasSize(expected.Length);

        sut.Update(CreateDisabledProperty(80));
        sut.Update(CreateRunningProperty(105));

        var now = publicationPeriod * 2;
        sut.Update(now);
        publishResult = sut.Publish(now);

        Check.That(publishResult.IsSome).IsTrue();

        expected = new IDataModelValue[]
        {
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 25f, 60, 120),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 35f, 60, 120),
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1f, 60, 120),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2f, 60, 120)
        };

        Check.That(publishResult.GetValue()).Contains(expected);
        Check.That(publishResult.GetValue()).HasSize(expected.Length);
    }

    #region Helpers

    private StateMonitoringComputer CreateSut(TestCaseId caseId, TimeSpan publicationPeriod, TimeSpan? windowPeriod = null, Type enumType = null)
    {
        var eType = enumType ?? typeof(fake_model.PublicState);
        return caseId switch
        {
            TestCaseId.StateWithoutIncluding => new StateMonitoringComputer(OutputUrn, eType, publicationPeriod, windowPeriod,
                Array.Empty<MeasureRuntimeInfo>(), _tsReader, _tsWriter, TimeSpan.Zero),

            TestCaseId.StateWithOneVariation => new StateMonitoringComputer(OutputUrn, eType, publicationPeriod, windowPeriod,
                new[]
                {
                    new MeasureRuntimeInfo(new SubMetricDef(FakeIndex, MetricKind.Variation, fake_model.fake_index))
                }, _tsReader, _tsWriter, TimeSpan.Zero),

            TestCaseId.StateWithTwoVariations => new StateMonitoringComputer(OutputUrn, eType, publicationPeriod, windowPeriod,
                new[]
                {
                    new MeasureRuntimeInfo(new SubMetricDef(FakeIndex, MetricKind.Variation, fake_model.fake_index)),
                    new MeasureRuntimeInfo(new SubMetricDef(TemperatureDelta, MetricKind.Variation, fake_model.temperature.measure))
                }, _tsReader, _tsWriter, TimeSpan.Zero),

            TestCaseId.StateWithAccumulator => new StateMonitoringComputer(OutputUrn, eType, publicationPeriod, windowPeriod,
                new[]
                {
                    new MeasureRuntimeInfo(new SubMetricDef(TemperatureAcc, MetricKind.SampleAccumulator, fake_model.temperature.measure))
                }, _tsReader, _tsWriter, TimeSpan.Zero),

            TestCaseId.StateWithVariationAndAccumulator => new StateMonitoringComputer(OutputUrn, eType, publicationPeriod, windowPeriod,
                new[]
                {
                    new MeasureRuntimeInfo(new SubMetricDef(FakeIndex, MetricKind.Variation, fake_model.fake_index)),
                    new MeasureRuntimeInfo(new SubMetricDef(TemperatureAcc, MetricKind.SampleAccumulator, fake_model.temperature.measure))
                }, _tsReader, _tsWriter, TimeSpan.Zero),

            _ => throw new ArgumentOutOfRangeException(nameof(caseId), caseId, null)
        };
    }

    private enum TestCaseId
    {
        StateWithoutIncluding,
        StateWithOneVariation,
        StateWithTwoVariations,
        StateWithAccumulator,
        StateWithVariationAndAccumulator
    }

    private static Property<fake_model.PublicState> CreatePublicStateProperty(fake_model.PublicState targetState, int atInMinutes)
        => Property<fake_model.PublicState>.Create(InputProperty, targetState, TimeSpan.FromMinutes(atInMinutes));

    private static Property<fake_model.PublicState> CreateRunningProperty(int atInMinutes)
        => CreatePublicStateProperty(fake_model.PublicState.Running, atInMinutes);

    private static Property<fake_model.PublicState> CreateDisabledProperty(int atInMinutes)
        => CreatePublicStateProperty(fake_model.PublicState.Disabled, atInMinutes);

    private static Property<Flow> CreateFakeIndexProperty(float value, int atInMinutes)
        => Property<Flow>.Create(fake_model.fake_index, Flow.FromFloat(value).Value, TimeSpan.FromMinutes(atInMinutes));

    private static Property<Temperature> CreateTemperatureProperty(float value, int atInMinutes)
        => Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(value),
            TimeSpan.FromMinutes(atInMinutes));

    #endregion
}