using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.Metrics.Tests.Helpers;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class StateMonitoringVariationMeasureTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();
    private readonly string Running_baseUrn = MetricUrn.Build("myOutputUrn", fake_model.PublicState.Running.ToString(), "myMeasureName");
    private readonly string Disabled_baseUrn = MetricUrn.Build("myOutputUrn", fake_model.PublicState.Disabled.ToString(), "myMeasureName");



    [SetUp]
    public void Init()
    {
        var dbPath = "/tmp/state_computer_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;    }

    [Test]
    public void GivenNoUpdate_WhenIGetItemsToPublish_ThenNoPropertyToPublish()
    {
        var sut = CreateSut(T._3, null, T._0);
        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        Check.That(publish).IsEmpty();
    }

    [Test]
    public void GivenStateDoesNotChanged_WhenIGetItemsToPublish()
    {
        var sut = CreateSut(T._3, null, T._0);

        //   Publish:            PUB         PUB
        //         T: 0   1   2   3   4   5   6  
        //     State: RUN
        //      data: 100 80      10  60  25
        // Variation:            -20         -55 (25-80)
        //    Global:                        -75 (25-100)

        const fake_model.PublicState curState = fake_model.PublicState.Running;
        sut.Update(100f, T._0, curState);
        sut.Update(80f, T._1, curState);
        sut.Update(10f, T._3, curState);

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, 0, 0, 3),
            PDH.CreateMetricValueProperty(Running_baseUrn, -20, 0, 3)
        );

        sut.Update(60f, T._4, curState);
        sut.Update(25f, T._5, curState);

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, 0, 3, 6),
            PDH.CreateMetricValueProperty(Running_baseUrn, -55, 3, 6)
        );
    }

    [Test]
    public void GivenWindowBiggerThanMetricPeriod()
    {
        var sut = CreateSut(T._3, T._9, T._0);

        //          Window:                                     |                            
        //         Publish:             |           |           |             |              |              |
        //      Temps(min): 0   1   2   3   4   5   6   7   8   9   10   11   12   13   14   15   16   17   18 
        //           State:     RUN     DIS         RUN         DIS           RUN  DIS                 RUN 
        //       FakeIndex:     5  10   4   8       11  13      17  21   7    9    24        3         4
        //PUB -------------
        //VAR on RUN state:             5(10-5)     5(10-5)     10(5+13-8)    5(13-8)        7(13-8+9-7)     3(9-7+4-3)
        //VAR on DIS state:             0          -2(8-10)    -2(8-10)      -8(8-10+7-13)   9(7-13+24-9)  -12(7-13+3-9)
        //          Global:             5           3(8-5)      8(13-5)      -3(7-10)       16(24-8)        -9(4-13)

        var curState = fake_model.PublicState.Running;
        sut.Update(5f, T._1, curState);
        sut.Update(10f, T._2, curState);

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, 0, 0, 3),
            PDH.CreateMetricValueProperty(Running_baseUrn, 5, 0, 3)
        );

        curState = fake_model.PublicState.Disabled;
        sut.Update(4f, T._3, curState);
        sut.Update(8f, T._4, curState);

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, -2, 0, 6),
            PDH.CreateMetricValueProperty(Running_baseUrn, 5, 0, 6)
        );

        curState = fake_model.PublicState.Running;
        sut.Update(11f, T._6, curState);
        sut.Update(13f, T._7, curState);

        sut.Update(T._9);
        publish = sut.GetItemsToPublish(T._9);
        sut.OnPublishDone(T._9);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, -2, 0, 9),
            PDH.CreateMetricValueProperty(Running_baseUrn, 10, 0, 9)
        );

        curState = fake_model.PublicState.Disabled;
        sut.Update(17f, T._9, curState);
        sut.Update(21f, T._10, curState);
        sut.Update(7f, T._11, curState);
        sut.Update(9f, T._12, fake_model.PublicState.Running);

        sut.Update(T._12);
        publish = sut.GetItemsToPublish(T._12);
        sut.OnPublishDone(T._12);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, -8, 3, 12),
            PDH.CreateMetricValueProperty(Running_baseUrn, 5, 3, 12)
        );

        curState = fake_model.PublicState.Disabled;
        sut.Update(24f, T._13, curState);
        sut.Update(3f, T._15, curState);

        sut.Update(T._15);
        publish = sut.GetItemsToPublish(T._15);
        sut.OnPublishDone(T._15);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, 9, 6, 15),
            PDH.CreateMetricValueProperty(Running_baseUrn, 7, 6, 15)
        );

        sut.Update(4f, T._17, fake_model.PublicState.Running);

        sut.Update(T._18);
        publish = sut.GetItemsToPublish(T._18);
        sut.OnPublishDone(T._18);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, -12, 9, 18),
            PDH.CreateMetricValueProperty(Running_baseUrn, 3, 9, 18)
        );
    }

    [Test]
    public void GivenVariation_OneValueOnRunningAndOneValueOnDisabled_WhenIPublish_ThenIComputeVariationWithPreviousValueKnown()
    {
        var sut = CreateSut(T._10, null, T._0);

        //      Temps(min): 0   1   2   3   4  10
        //           State: RUN         DIS
        //       FakeIndex:     5           8
        //PUB FakeIndex---
        //   For RUN state:                    0 not enough values
        //   For DIS state:                    3 (8-5)  

        sut.Update(5f, T._1, fake_model.PublicState.Running);
        sut.Update(8f, T._4, fake_model.PublicState.Disabled);

        sut.Update(T._10);
        var publish = sut.GetItemsToPublish(T._10);
        sut.OnPublishDone(T._10);
        Check.That(publish).ContainsExactly(
            PDH.CreateMetricValueProperty(Disabled_baseUrn, 3, 0, 10),
            PDH.CreateMetricValueProperty(Running_baseUrn, 0, 0, 10)
        );
    }

    #region Helpers

    private StateMonitoringVariationMeasure CreateSut(
        TimeSpan publicationPeriod,
        TimeSpan? windowPeriod,
        TimeSpan now)
        => new (MetricUrn.Build("myOutputUrn"), typeof(fake_model.PublicState), "myMeasureName", publicationPeriod, windowPeriod, _tsReader, _tsWriter, now);

    #endregion
}