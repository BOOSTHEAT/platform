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
public class StateMonitoringOccurenceDurationMeasureTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();
    private static readonly MetricUrn StateMetricUrn = fake_analytics_model.public_state_A2;
    private readonly MetricUrn Running_BaseUrn = MetricUrn.Build(StateMetricUrn, fake_model.PublicState.Running.ToString());
    private readonly MetricUrn Disabled_BaseUrn = MetricUrn.Build(StateMetricUrn, fake_model.PublicState.Disabled.ToString());



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

    [Test]
    public void GivenNeverUpdate_WhenIGetItemsToPublish_ThenNoPropertiesToPublish()
    {
        var sut = CreateSut(StateMetricUrn, T._5, null, T._0);
        var publish = sut.GetItemsToPublish(T._5);
        Check.That(publish).IsEqualTo(Array.Empty<IDataModelValue>());
    }

    [Test]
    public void GivenNoStateUpdate_WhenIGetItemsToPublish_ThenNothingIsPublished()
    {
        var sut = CreateSut(StateMetricUrn, T._3, null, T._0);

        // PUB                       |
        //            T: 0   1   2   3  
        //        State:              
        //  Pub RUN occ:             -
        //  Pub RUN dur:             -
        //  Pub DIS occ:             - 
        //  Pub DIS dur:             -

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).IsEmpty();
    }

    [Test]
    public void GivenNoStateUpdateFromMultiplePublish_WhenIGetItemsToPublish_ThenShouldGetDurationEqualsToPublicationPeriod()
    {
        var sut = CreateSut(StateMetricUrn, T._3, null, T._0);

        //            T: 0   1   2   3   4   5   6   7   8   9 
        //        State: RUN             
        //  Pub RUN occ:             1           1           1
        //  Pub RUN dur:             3           3           3
        //  Pub DIS occ:             0           0           0
        //  Pub DIS dur:             0           0           0

        sut.Update(0, T._0, fake_model.PublicState.Running);
        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 3),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 3),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 0, 3)
        );

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 3, 6),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 3, 6),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 3, 6),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 3, 6)
        );

        sut.Update(T._9);
        publish = sut.GetItemsToPublish(T._9);
        sut.OnPublishDone(T._9);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 6, 9),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 6, 9),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 6, 9),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 6, 9)
        );
    }

    [Test]
    public void GivenWindowed_NoStateUpdateFromMultiplePublish_WhenIGetItemsToPublish_ThenShouldGetDurationEqualsToWindowPeriod()
    {
        var sut = CreateSut(StateMetricUrn, T._3, T._6, T._0);

        //            T: 0   1   2   3   4   5   6   7   8   9 
        //        State: RUN             
        //  Pub RUN occ:             1           1           1
        //  Pub RUN dur:             3           6           6
        //  Pub DIS occ:             0           0           0
        //  Pub DIS dur:             0           0           0

        sut.Update(0, T._0, fake_model.PublicState.Running);
        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 3),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 3),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 0, 3)
        );

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 0, 6),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 0, 6),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 6),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 6, 0, 6)
        );

        sut.Update(T._9);
        publish = sut.GetItemsToPublish(T._9);
        sut.OnPublishDone(T._9);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 0, 3, 9),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 3, 9),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 3, 9),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 6, 3, 9)
        );
    }

    [Test]
    public void GivenNominalCases_WhenIGetItemsToPublish()
    {
        var sut = CreateSut(StateMetricUrn, T._3, null, T._0);

        //            T: 0   1   2   3   4   5   6   7   8   9   10  11  12 
        //        State: RUN    DIS RUN              DIS     RUN RUN RUN
        //  Pub RUN occ:             1           1           1           1 
        //  Pub RUN dur:             2           3           1           3 
        //  Pub DIS occ:             1           1           1           1        
        //  Pub DIS dur:             1           0           2           0       

        sut.Update(0, T._0, fake_model.PublicState.Running);
        sut.Update(0, T._2, fake_model.PublicState.Disabled);

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 1, 0, 3),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 2, 0, 3)
        );

        sut.Update(0, T._3, fake_model.PublicState.Running);

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 3, 6),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 3, 6),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 3, 6),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 3, 6)
        );

        sut.Update(0, T._7, fake_model.PublicState.Disabled);
        sut.Update(0, T._9, fake_model.PublicState.Running);

        sut.Update(T._9);
        publish = sut.GetItemsToPublish(T._9);
        sut.OnPublishDone(T._9);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 6, 9),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 2, 6, 9),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 6, 9),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 1, 6, 9)
        );

        sut.Update(0, T._10, fake_model.PublicState.Running);
        sut.Update(0, T._11, fake_model.PublicState.Running);

        sut.Update(T._12);
        publish = sut.GetItemsToPublish(T._12);
        sut.OnPublishDone(T._12);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 9, 12),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 0, 9, 12),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 9, 12),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 9, 12)
        );
    }

    [Test]
    public void GivenWindowed_WhenOldestStateBecomeOutsideWindow_AndNoStateChangedOnSamplingStart_ThenIShouldCountOldestOccurenceOnSamplingStart()
    {
        var sut = CreateSut(StateMetricUrn, T._4, T._8, T._0);

        //         Publish:                 |               |                 |                   |
        //      Temps(min): 0   1   2   3   4   5   6   7   8   9   10   11   12   13   14   15   16
        //           State: RUN         DIS         RUN         DIS                     RUN         
        //PUB -------------
        //   RUN occurence:                 1               2                 1                   2
        //   RUN dura(min):                 3               5(3-0 + 8-6)      3(9-6)              3(9-8 + 16-14)
        //   DIS occurence:                 1               1                 2                   1
        //   DIS dura(min):                 1               3(6-3)            5(6-4 + 12-9)       5(14-9)

        sut.Update(0, T._0, fake_model.PublicState.Running);
        sut.Update(0, T._3, fake_model.PublicState.Disabled);

        sut.Update(T._4);
        sut.GetItemsToPublish(T._4);
        sut.OnPublishDone(T._4);

        sut.Update(0, T._6, fake_model.PublicState.Running);

        sut.Update(T._8);
        sut.GetItemsToPublish(T._8);
        sut.OnPublishDone(T._8);

        sut.Update(0, T._9, fake_model.PublicState.Disabled);

        sut.Update(T._12);
        var publish = sut.GetItemsToPublish(T._12);
        sut.OnPublishDone(T._12);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 2, 4, 12),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 5, 4, 12),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 4, 12),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 4, 12)
        );

        sut.Update(0, T._14, fake_model.PublicState.Running);

        sut.Update(T._16);
        publish = sut.GetItemsToPublish(T._16);
        sut.OnPublishDone(T._16);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 8, 16),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 5, 8, 16),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2, 8, 16),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 3, 8, 16)
        );
    }

    [Test]
    public void WindowedNominalCases_GetItemsToPublish()
    {
        var sut = CreateSut(StateMetricUrn, T._3, T._6, T._0);

        //            T: 0   1   2   3   4   5   6   7   8   9   10  11  12  
        //        State: RUN    DIS RUN              DIS     RUN RUN RUN
        //  Pub RUN occ:             1           2           1           2           
        //  Pub RUN dur:             2           5(2+3)      4(7-4)      4(7-6+12-9) 
        //  Pub DIS occ:             1           1           1           1           
        //  Pub DIS dur:             1           1           2(9-7)      2(9-7)      

        sut.Update(0, T._0, fake_model.PublicState.Running);
        sut.Update(0, T._2, fake_model.PublicState.Disabled);

        sut.Update(T._3);
        var publish = sut.GetItemsToPublish(T._3);
        sut.OnPublishDone(T._3);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 1, 0, 3),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 0, 3),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 2, 0, 3)
        );

        sut.Update(0, T._3, fake_model.PublicState.Running);

        sut.Update(T._6);
        publish = sut.GetItemsToPublish(T._6);
        sut.OnPublishDone(T._6);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 0, 6),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 1, 0, 6),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2, 0, 6),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 5, 0, 6)
        );

        sut.Update(0, T._7, fake_model.PublicState.Disabled);
        sut.Update(0, T._9, fake_model.PublicState.Running);

        sut.Update(T._9);
        publish = sut.GetItemsToPublish(T._9);
        sut.OnPublishDone(T._9);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 3, 9),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 2, 3, 9),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 1, 3, 9),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 4, 3, 9)
        );

        sut.Update(0, T._10, fake_model.PublicState.Running);
        sut.Update(0, T._11, fake_model.PublicState.Running);

        sut.Update(T._12);
        publish = sut.GetItemsToPublish(T._12);
        sut.OnPublishDone(T._12);
        Check.That(publish).ContainsExactly(
            PDH.CreateStateOccurenceProperty(Disabled_BaseUrn, 1, 6, 12),
            PDH.CreateStateDurationProperty(Disabled_BaseUrn, 2, 6, 12),
            PDH.CreateStateOccurenceProperty(Running_BaseUrn, 2, 6, 12),
            PDH.CreateStateDurationProperty(Running_BaseUrn, 4, 6, 12)
        );
    }

    private StateMonitoringOccurenceDurationMeasure CreateSut(MetricUrn baseOutputUrn, TimeSpan publicationPeriod, TimeSpan? windowPeriod, TimeSpan now)
        => new (baseOutputUrn, typeof(fake_model.PublicState), MetricUrn.OCCURRENCE, publicationPeriod, windowPeriod, _tsReader, _tsWriter, now);
}