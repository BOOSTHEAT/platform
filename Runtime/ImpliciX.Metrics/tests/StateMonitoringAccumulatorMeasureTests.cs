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
public class StateMonitoringAccumulatorMeasureTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();
    private readonly string Running_baseUrn = MetricUrn.Build("myOutputUrn", fake_model.PublicState.Running.ToString(), "myMeasureName");
    private readonly string Disabled_baseUrn = MetricUrn.Build("myOutputUrn", fake_model.PublicState.Disabled.ToString(), "myMeasureName");


    [SetUp]
    public void Init()
    {
        var dbPath = "/tmp/state_accumumlator_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
    }

    [Test]
    public void NominalCase()
    {
        var sut = CreateSut(T._4, null, T._0);

        //           Pub:                 |               |
        //             T: 0   1   2   3   4   5   6   7   8
        //         State: DIS             RUN
        //          data: 8       4       7       8   1
        // DIS Acc value:                12               0
        // DIS Acc count:                 2               0
        // RUN Acc value:                 0               16
        // RUN Acc count:                 0               3

        var curState = fake_model.PublicState.Disabled;
        sut.Update(8f, T._0, curState);
        sut.Update(4f, T._2, curState);

        curState = fake_model.PublicState.Running;
        sut.Update(7f, T._4, curState);

        sut.Update(T._4);
        var publish = sut.GetItemsToPublish(T._4);
        sut.OnPublishDone(T._4);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 2, 0, 4),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 12, 0, 4),
            PDH.CreateAccumulatorCount(Running_baseUrn, 0, 0, 4),
            PDH.CreateAccumulatorValue(Running_baseUrn, 0, 0, 4)
        );

        sut.Update(7f, T._4, curState);
        sut.Update(8f, T._6, curState);
        sut.Update(1f, T._7, curState);

        sut.Update(T._8);
        publish = sut.GetItemsToPublish(T._8);
        sut.OnPublishDone(T._8);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 0, 4, 8),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 0, 4, 8),
            PDH.CreateAccumulatorCount(Running_baseUrn, 3, 4, 8),
            PDH.CreateAccumulatorValue(Running_baseUrn, 16, 4, 8)
        );
    }

    [Test]
    public void GivenWindowBiggerThanMetricPeriod()
    {
        var sut = CreateSut(T._3, T._9, T._0);

        //        Window:                                     |                            
        //       Publish:             |           |           |             |              |              |
        //    Temps(min): 0   1   2   3   4   5   6   7   8   9   10   11   12   13   14   15   16   17   18 
        //         State:     RUN     DIS         RUN         DIS           RUN  DIS                 RUN 
        //     FakeIndex:     5  10   4   8       11  13      17  21   7    9    24        3         4
        //PUB -------------
        // DIS Acc value:             0           12          12            57(4+8+45)     69(45+24)      72(45+27)
        // DIS Acc count:             0           2           2             5              4              5
        // RUN Acc value:             15          15          39            24             33(24+9)       13(9+4)
        // RUN Acc count:             2           2           4             2              3              2

        var curState = fake_model.PublicState.Running;
        sut.Update(5f, T._1, curState);
        sut.Update(10f, T._2, curState);

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 0, 0, 3),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 0, 0, 3),
            PDH.CreateAccumulatorCount(Running_baseUrn, 2, 0, 3),
            PDH.CreateAccumulatorValue(Running_baseUrn, 15, 0, 3)
        );

        curState = fake_model.PublicState.Disabled;
        sut.Update(4f, T._3, curState);
        sut.Update(8f, T._4, curState);

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 2, 0, 6),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 12, 0, 6),
            PDH.CreateAccumulatorCount(Running_baseUrn, 2, 0, 6),
            PDH.CreateAccumulatorValue(Running_baseUrn, 15, 0, 6)
        );

        curState = fake_model.PublicState.Running;
        sut.Update(11f, T._6, curState);
        sut.Update(13f, T._7, curState);

        sut.Update(T._9);
        publish = sut.GetItemsToPublish(T._9);
        sut.OnPublishDone(T._9);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 2, 0, 9),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 12, 0, 9),
            PDH.CreateAccumulatorCount(Running_baseUrn, 4, 0, 9),
            PDH.CreateAccumulatorValue(Running_baseUrn, 39, 0, 9)
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
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 5, 3, 12),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 57, 3, 12),
            PDH.CreateAccumulatorCount(Running_baseUrn, 2, 3, 12),
            PDH.CreateAccumulatorValue(Running_baseUrn, 24, 3, 12)
        );

        curState = fake_model.PublicState.Disabled;
        sut.Update(24f, T._13, curState);
        sut.Update(3f, T._15, curState);

        sut.Update(T._15);
        publish = sut.GetItemsToPublish(T._15);
        sut.OnPublishDone(T._15);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 4, 6, 15),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 69, 6, 15),
            PDH.CreateAccumulatorCount(Running_baseUrn, 3, 6, 15),
            PDH.CreateAccumulatorValue(Running_baseUrn, 33, 6, 15)
        );

        sut.Update(4f, T._17, fake_model.PublicState.Running);

        sut.Update(T._18);
        publish = sut.GetItemsToPublish(T._18);
        sut.OnPublishDone(T._18);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 5, 9, 18),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 72, 9, 18),
            PDH.CreateAccumulatorCount(Running_baseUrn, 2, 9, 18),
            PDH.CreateAccumulatorValue(Running_baseUrn, 13, 9, 18)
        );
    }

    [Test]
    public void GivenNoUpdate_WhenIGetItemsToPublish_ThenAllAccumulatedValueAndSamplesCountAreEqualsToZero()
    {
        var sut = CreateSut(T._3, null, T._0);

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateAccumulatorCount(Disabled_baseUrn, 0, 0, 3),
            PDH.CreateAccumulatorValue(Disabled_baseUrn, 0, 0, 3),
            PDH.CreateAccumulatorCount(Running_baseUrn, 0, 0, 3),
            PDH.CreateAccumulatorValue(Running_baseUrn, 0, 0, 3)
        );
    }

    #region Helpers

    private StateMonitoringAccumulatorMeasure CreateSut(
        TimeSpan publicationPeriod,
        TimeSpan? windowPeriod,
        TimeSpan now)
        => new (
            MetricUrn.Build("myOutputUrn"),
            typeof(fake_model.PublicState),
            "myMeasureName",
            publicationPeriod,
            windowPeriod,
            _tsReader,
            _tsWriter,
            now
        );

    #endregion
}
