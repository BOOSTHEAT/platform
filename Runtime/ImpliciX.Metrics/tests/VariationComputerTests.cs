using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class VariationComputerTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();

    [SetUp]
    public void Init()
    {
        var dbPath = "/tmp/variation_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
    }

    #region WhenIPublish

    [Test]
    public void WhenInputValuesIsEmpty_WhenIPublish_ThenIGetDeltaEqualsTo0()
    {
        var outputUrn = MetricUrn.Build("foo:variation");
        var publicationPeriod = T._3;
        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        //  Temps(min): 0  1  2  3
        //       Value: 
        //Publish 3min:          0

        sut.Update(publicationPeriod);
        var outcome = sut.Publish(T._3).GetValue().Single();
        var value = ((IFloat) outcome.ModelValue()).ToFloat();
        Check.That(value).IsEqualTo(0f);
    }

    [Test]
    public void Nominal_case()
    {
        const string inputUrn = "foo:measure";
        var publicationPeriod = T._3;
        var outputUrn = MetricUrn.Build("foo:variation");
        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        //  Temps(min): 0  1  2  3  4  5  6
        //       Value: 1  2  5  18 6  10
        //Publish 3min:          4        5 = 9 (4+5)
        //      Global:                       9

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 2, T._1));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

        sut.Update(publicationPeriod);
        var outcome = sut.Publish(T._3).GetValue().Single();
        var value = ((IFloat) outcome.ModelValue()).ToFloat();
        Check.That(value).IsEqualTo(4f);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 18, T._3));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 6, T._4));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 10, T._5));

        sut.Update(publicationPeriod * 2);
        outcome = sut.Publish(publicationPeriod * 2).GetValue().Single();
        value = ((IFloat) outcome.ModelValue()).ToFloat();
        Check.That(value).IsEqualTo(5f);
    }

    [Test]
    public void windowed_nominal_case()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:windowed:variation");
        var publicationPeriod = T._3;
        var sut = CreateSut(outputUrn, publicationPeriod, publicationPeriod * 2, T._0);

        //        Pub:          |           |           |
        // Temps(min): 0  1  2  3   4   5   6   7   8   9
        //      Value: 1  2  5  18  15  10  12  9
        //  Variation:          4(5-1)      9(10-1)     4(9-5)    
        //     Global:                                  8(9-1)

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 2, T._1));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

        sut.Update(publicationPeriod);
        var outcome = sut.Publish(publicationPeriod).GetValue().Single();
        var expected = PDH.CreateMetricValueProperty(new[] {outputUrn.Value}, 4f, 0, 3);
        PDH.CheckAreEquals((Property<MetricValue>) outcome, expected);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 18, T._3));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 15, T._4));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 10, T._5));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 12, T._6));

        sut.Update(publicationPeriod * 2);
        outcome = sut.Publish(publicationPeriod * 2).GetValue().Single();
        expected = PDH.CreateMetricValueProperty(new[] {outputUrn.Value}, 9f, 0, 6);
        PDH.CheckAreEquals((Property<MetricValue>) outcome, expected);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 9, T._7));

        sut.Update(publicationPeriod * 3);
        outcome = sut.Publish(publicationPeriod * 3).GetValue().Single();
        expected = PDH.CreateMetricValueProperty(new[] {outputUrn.Value}, 4f, 3, 9);
        PDH.CheckAreEquals((Property<MetricValue>) outcome, expected);
    }

    [Test]
    public void WhenIReceiveOnlyOneUrnUpdateDuringMetricPeriod_WhenIPublish_ThenIShouldUsePreviousLastValueAsFirstValue()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:variation");
        var publicationPeriod = T._3;
        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        //  Temps(min): 0 1 2 3 4 5 6
        //       Value: 1   5   8
        //Publish 3min:       4     3 (8-5)

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));
        sut.Publish(publicationPeriod);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._4));
        sut.Update(publicationPeriod * 2);
        var outcome = (Property<MetricValue>) sut.Publish(publicationPeriod * 2).GetValue().Single();
        var expected = PDH.CreateMetricValueProperty(outputUrn, value: 3f, startInMinutes: 3, endInMinutes: 6, atInMinutes: 6);
        PDH.CheckAreEquals(outcome, expected);
    }

    [Test]
    public void GivenPreviousUrnValueIsKnownBeforeCurrentPeriod_AndIDoNotReceivedUrnUpdateDuringCurrentPeriod_WhenIPublish_ThenVariationIsEqualsTo0()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:variation");
        var publicationPeriod = TimeSpan.FromMinutes(3);
        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        //  Temps(min): 0 1 2 3 4 5 6
        //       Value: 1   5    
        //Publish 3min:       4     0

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

        sut.Update(publicationPeriod);
        var outcome = (Property<MetricValue>) sut.Publish(publicationPeriod).GetValue().Single();
        var expected = PDH.CreateMetricValueProperty(outputUrn, value: 4f, startInMinutes: 0, endInMinutes: 3, atInMinutes: 3);
        PDH.CheckAreEquals(outcome, expected);

        sut.Update(publicationPeriod * 2);
        outcome = (Property<MetricValue>) sut.Publish(publicationPeriod * 2).GetValue().Single();
        expected = PDH.CreateMetricValueProperty(outputUrn, value: 0f, startInMinutes: 3, endInMinutes: 6, atInMinutes: 6);
        PDH.CheckAreEquals(outcome, expected);
    }

    [Test]
    public void
        GivenLastOnPreviousPublishWasGetFromTwoPublishAgo_AndNoUpdateDuringThePreviousPeriod_AndIGetUpdateOnCurrentPeriod_WhenIPublish_ThenLastOnPreviousPublishIsUseForCurrentPeriod()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:variation");
        var publicationPeriod = TimeSpan.FromMinutes(3);
        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        //  Temps(min): 0 1 2 3 4 5 6 7 8 9
        //       Value: 1   5         8
        //Publish 3min:       4     0     3

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

        sut.Update(publicationPeriod);
        var outcome = (Property<MetricValue>) sut.Publish(publicationPeriod).GetValue().Single();
        var expected = PDH.CreateMetricValueProperty(outputUrn, value: 4f, startInMinutes: 0, endInMinutes: 3, atInMinutes: 3);
        PDH.CheckAreEquals(outcome, expected);

        sut.Update(publicationPeriod * 2);
        outcome = (Property<MetricValue>) sut.Publish(publicationPeriod * 2).GetValue().Single();
        expected = PDH.CreateMetricValueProperty(outputUrn, value: 0f, startInMinutes: 3, endInMinutes: 6, atInMinutes: 6);
        PDH.CheckAreEquals(outcome, expected);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._7));

        sut.Update(publicationPeriod * 3);
        outcome = (Property<MetricValue>) sut.Publish(publicationPeriod * 3).GetValue().Single();
        expected = PDH.CreateMetricValueProperty(outputUrn, value: 3f, startInMinutes: 6, endInMinutes: 9, atInMinutes: 9);
        PDH.CheckAreEquals(outcome, expected);
    }

    [Test]
    public void GivenPreviousUrnValueIsUnknownBeforeCurrentPeriod_AndIHaveOnlyOneUpdateDuringCurrentPeriod_WhenIPublish_ThenVariationIsEqualsTo0()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:variation");
        var publicationPeriod = TimeSpan.FromMinutes(3);
        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        //  Temps(min): 0 1 2 3
        //       Value:     5  
        //Publish 3min:       0

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));
        var outcome = (Property<MetricValue>) sut.Publish(publicationPeriod).GetValue().Single();
        var expected = PDH.CreateMetricValueProperty(outputUrn, value: 0f, startInMinutes: 0, endInMinutes: 2, atInMinutes: 3);
        PDH.CheckAreEquals(outcome, expected);
    }

    [Test]
    public void GivenUpdateAtIsReceivedAfterTheLastUpdateValue_WhenIPublish_ThenSamplingEndDateHasTheUpdateAtTimeSpan()
    {
        // Given
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:variation");
        var publicationPeriod = TimeSpan.FromSeconds(1);

        var sut = CreateSut(outputUrn, publicationPeriod, null, T._0);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 100f, TimeSpan.FromMilliseconds(200)));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 145f, TimeSpan.FromMilliseconds(700)));

        var lastSystemTickedValue = TimeSpan.FromMilliseconds(900);
        sut.Update(lastSystemTickedValue);

        // When
        var dataToPublish = (Property<MetricValue>) sut.Publish(publicationPeriod).GetValue().First();

        // Then
        Check.That(dataToPublish.At).IsEqualTo(publicationPeriod);
        Check.That(dataToPublish.Urn.Value).IsEqualTo(outputUrn);
        Check.That(dataToPublish.Value.Value).IsEqualTo(45f);
        Check.That(dataToPublish.Value.SamplingStartDate).IsEqualTo(T._0);
        Check.That(dataToPublish.Value.SamplingEndDate).IsEqualTo(lastSystemTickedValue);
    }

    #endregion

    private VariationComputer CreateSut(MetricUrn outputUrn, TimeSpan publicationPeriod, TimeSpan? windowPeriod, TimeSpan now)
        => new (outputUrn, publicationPeriod, windowPeriod, _tsReader, _tsWriter, now);
}