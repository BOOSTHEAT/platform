using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Computers.Variation;
using NUnit.Framework;
using PDH = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.TimeMath.Tests;

public class VariationTests
{
  private const string DbName = "computers";

  private ITimeMathReader _timeMathReader;
  private ITimeMathWriter _timeMathWriter;
  private readonly TimeHelper T = TimeHelper.Minutes();

  [SetUp]
  public void Init()
  {
    var tmpComputersDbFolder = Path.Combine(Path.GetTempPath(), nameof(VariationTests));
    if (Directory.Exists(tmpComputersDbFolder))
      Directory.Delete(tmpComputersDbFolder, true);

    var tsDb = new TimeSeriesDb(tmpComputersDbFolder, DbName);
    _timeMathReader = new TimeBasedTimeMathReader(tsDb);
    _timeMathWriter = new TimeBasedTimeMathWriter(tsDb);
  }

  [Test]
  public void WhenInputValuesIsEmpty_WhenIPublish_ThenIGetDeltaEqualsTo0()
  {
    var outputUrn = MetricUrn.Build("foo:variation");
    var publicationPeriod = T._3;
    var sut = CreateSut(outputUrn, T._0);

    //   Time(min): 0  1  2  3
    //       Value: 
    //Publish 3min:          0

    sut.Update(publicationPeriod);
    var outcome = sut.Publish(T._3).GetValue().Single();
    var expected = PDH.CreateMetricValueProperty(new[] {outputUrn.Value}, 0, 0, 3);
    PDH.CheckAreEquals(outcome, expected);
  }

  [Test]
  public void WhenInputValuesIsSingle_WhenIPublish_ThenIGetDeltaEqualsTo0()
  {
    const string inputUrn = "foo:measure";
    var outputUrn = MetricUrn.Build("foo:variation");
    var publicationPeriod = T._3;
    var sut = CreateSut(outputUrn, T._0);

    //   Time(min): 0  1  2  3
    //       Value:       10
    //Publish 3min:          0

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 10, T._2));
    sut.Update(publicationPeriod);
    var outcome = sut.Publish(T._3).GetValue().Single();
    var expected = PDH.CreateMetricValueProperty(new[] {outputUrn.Value}, 0, 0, 3);
    PDH.CheckAreEquals(outcome, expected);
  }

  [Test]
  public void GivenIReceiveOnlyOneUrnUpdateDuringMetricPeriod_WhenIPublish_ThenIShouldUsePreviousLastValueAsFirstValue()
  {
    const string inputUrn = "foo:measure";
    var outputUrn = MetricUrn.Build("foo:variation");
    var publicationPeriod = T._3;
    var sut = CreateSut(outputUrn, T._0);

    //   Time(min): 0 1 2 3 4 5 6
    //       Value: 1   5   8
    //Publish 3min:       4     3 (8-5)

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));
    sut.Publish(publicationPeriod);

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._4));
    sut.Update(publicationPeriod * 2);
    var outcome = sut.Publish(publicationPeriod * 2).GetValue().Single();
    var expected = PDH.CreateMetricValueProperty(outputUrn, value: 3f, startInMinutes: 3, endInMinutes: 6, atInMinutes: 6);
    PDH.CheckAreEquals(outcome, expected);
  }

  [Test]
  public void GivenPreviousUrnValueIsKnownBeforeCurrentPeriod_AndIDoNotReceivedUrnUpdateDuringCurrentPeriod_WhenIPublish_ThenVariationIsEqualsTo0()
  {
    const string inputUrn = "foo:measure";
    var outputUrn = MetricUrn.Build("foo:variation");
    var publicationPeriod = TimeSpan.FromMinutes(3);
    var sut = CreateSut(outputUrn, T._0);

    //   Time(min): 0 1 2 3 4 5 6
    //       Value: 1   5    
    //Publish 3min:       4     0 = 4
    //      Global:             4

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

    sut.Update(publicationPeriod);
    var outcome = sut.Publish(publicationPeriod).GetValue().Single();
    var expected = PDH.CreateMetricValueProperty(outputUrn, value: 4f, startInMinutes: 0, endInMinutes: 3, atInMinutes: 3);
    PDH.CheckAreEquals(outcome, expected);

    sut.Update(publicationPeriod * 2);
    outcome = sut.Publish(publicationPeriod * 2).GetValue().Single();
    expected = PDH.CreateMetricValueProperty(outputUrn, value: 0f, startInMinutes: 3, endInMinutes: 6, atInMinutes: 6);
    PDH.CheckAreEquals(outcome, expected);
  }

  [Test]
  public void
    GivenLastOfPreviousPublishWasGetFromTwoPublishAgo_AndNoUpdateDuringThePreviousPeriod_AndIGetUpdateOnCurrentPeriod_WhenIPublish_ThenLastOfPreviousPublishIsUseForCurrentPeriod()
  {
    const string inputUrn = "foo:measure";
    var outputUrn = MetricUrn.Build("foo:variation");
    var publicationPeriod = TimeSpan.FromMinutes(3);
    var sut = CreateSut(outputUrn, T._0);

    //   Time(min): 0 1 2 3 4 5 6 7 8 9
    //       Value: 1   5         8
    //Publish 3min:       4     0     3 = 7
    //      Global:                   7

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

    sut.Update(publicationPeriod);
    var outcome = sut.Publish(publicationPeriod).GetValue().Single();
    var expected = PDH.CreateMetricValueProperty(outputUrn, value: 4f, startInMinutes: 0, endInMinutes: 3, atInMinutes: 3);
    PDH.CheckAreEquals(outcome, expected);

    sut.Update(publicationPeriod * 2);
    outcome = sut.Publish(publicationPeriod * 2).GetValue().Single();
    expected = PDH.CreateMetricValueProperty(outputUrn, value: 0f, startInMinutes: 3, endInMinutes: 6, atInMinutes: 6);
    PDH.CheckAreEquals(outcome, expected);

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._7));

    sut.Update(publicationPeriod * 3);
    outcome = sut.Publish(publicationPeriod * 3).GetValue().Single();
    expected = PDH.CreateMetricValueProperty(outputUrn, value: 3f, startInMinutes: 6, endInMinutes: 9, atInMinutes: 9);
    PDH.CheckAreEquals(outcome, expected);
  }

  [Test]
  public void GivenUpdateAtIsReceivedAfterTheLastUpdateValue_WhenIPublish_ThenSamplingEndDateIsEqualToTheUpdateAt()
  {
    const string inputUrn = "foo:measure";
    var outputUrn = MetricUrn.Build("foo:variation");
    var publicationPeriod = TimeSpan.FromSeconds(1);

    //    Time(ms): 0 200  700  900 1000
    //       Value:   100  145  
    //    UpdateAt:              |
    //Publish 1sec:                 45

    var sut = CreateSut(outputUrn, T._0);

    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 100f, TimeSpan.FromMilliseconds(200)));
    sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 145f, TimeSpan.FromMilliseconds(700)));

    var lastSystemTickedValue = TimeSpan.FromMilliseconds(900);
    sut.Update(lastSystemTickedValue);

    var outcome = sut.Publish(publicationPeriod).GetValue().First();
    var expected = PDH.CreateMetricValueProperty(outputUrn, value: 45f, T._0, lastSystemTickedValue, publicationPeriod);
    PDH.CheckAreEquals(outcome, expected);
  }

  private VariationComputer CreateSut(PropertyUrn<MetricValue> outputUrn, TimeSpan now)
    => new (outputUrn, _timeMathWriter, _timeMathReader, now);
}