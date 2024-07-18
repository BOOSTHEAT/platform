using System;
using System.Collections.Generic;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
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
public class AccumulatorComputerTests
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();
    

    [SetUp]
    public void Init()
    {
        var dbPath = "/tmp/accumumlator_tests";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
    }

    private readonly Dictionary<string, (Property<Temperature>[], Option<IDataModelValue[]>)> _testCases = new ()
    {
        ["no_value"] = (Array.Empty<Property<Temperature>>(),
                Option<IDataModelValue[]>.Some(new IDataModelValue[]
                    {
                        Property<MetricValue>.Create(
                            MetricUrn.BuildAccumulatedValue(fake_analytics_model.sample_metric.Value),
                            new MetricValue(0, TimeSpan.Zero, TimeSpan.FromHours(1)),
                            TimeSpan.FromHours(1)),
                        Property<MetricValue>.Create(
                            MetricUrn.BuildSamplesCount(fake_analytics_model.sample_metric.Value),
                            new MetricValue(0, TimeSpan.Zero, TimeSpan.FromHours(1)),
                            TimeSpan.FromHours(1))
                    }
                )
            ),
        ["simple"] = (
                new[]
                {
                    Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(42), TimeSpan.FromMinutes(1))
                },
                Option<IDataModelValue[]>.Some(new IDataModelValue[]
                    {
                        Property<MetricValue>.Create(
                            MetricUrn.BuildAccumulatedValue(fake_analytics_model.sample_metric.Value),
                            new MetricValue(42, TimeSpan.Zero, TimeSpan.FromHours(1)),
                            TimeSpan.FromHours(1)),
                        Property<MetricValue>.Create(
                            MetricUrn.BuildSamplesCount(fake_analytics_model.sample_metric.Value),
                            new MetricValue(1, TimeSpan.Zero, TimeSpan.FromHours(1)),
                            TimeSpan.FromHours(1))
                    }
                )
            ),
        ["multiple_values"] = (
            new[]
            {
                Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(42), TimeSpan.FromMinutes(1)),
                Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(19), TimeSpan.FromMinutes(2))
            },
            Option<IDataModelValue[]>.Some(new IDataModelValue[]
                {
                    Property<MetricValue>.Create(
                        MetricUrn.BuildAccumulatedValue(fake_analytics_model.sample_metric.Value),
                        new MetricValue(61, TimeSpan.Zero, TimeSpan.FromHours(1)),
                        TimeSpan.FromHours(1)),
                    Property<MetricValue>.Create(
                        MetricUrn.BuildSamplesCount(fake_analytics_model.sample_metric.Value),
                        new MetricValue(2, TimeSpan.Zero, TimeSpan.FromHours(1)),
                        TimeSpan.FromHours(1))
                }
            )),
        ["multiple_values_with_a_0"] = (
            new[]
            {
                Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(42), TimeSpan.FromMinutes(1)),
                Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(0), TimeSpan.FromMinutes(2)),
            },
            Option<IDataModelValue[]>.Some(new IDataModelValue[]
                {
                    Property<MetricValue>.Create(
                        MetricUrn.BuildAccumulatedValue(fake_analytics_model.sample_metric.Value),
                        new MetricValue(42, TimeSpan.Zero, TimeSpan.FromHours(1)),
                        TimeSpan.FromHours(1)),
                    Property<MetricValue>.Create(
                        MetricUrn.BuildSamplesCount(fake_analytics_model.sample_metric.Value),
                        new MetricValue(2, TimeSpan.Zero, TimeSpan.FromHours(1)), TimeSpan.FromHours(1))
                }
            ))
    };

    [TestCase("no_value")]
    [TestCase("simple")]
    [TestCase("multiple_values")]
    [TestCase("multiple_values_with_a_0")]
    public void should_update_values(string testName)
    {
        var (properties, expected) = _testCases[testName];

        var outputUrn = fake_analytics_model.sample_metric;
        var publicationPeriod = TimeSpan.FromHours(1);
        var sut = CreateSut(outputUrn, publicationPeriod, null, TimeSpan.Zero);

        foreach (var property in properties)
            sut.Update(property);

        sut.Update(publicationPeriod);
        var result = sut.Publish(publicationPeriod);
        Check.That(result.IsSome).IsEqualTo(expected.IsSome);
        Check.That(result.GetValue()).IsEqualTo(expected.GetValue());
    }

    [Test]
    public void GivenWindowBiggerThanMetricPeriod()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:windowed:accumulator");
        var publicationPeriod = T._3;

        var sut = CreateSut(outputUrn, publicationPeriod, publicationPeriod * 2, TimeSpan.Zero);

        //                     8           51          72         79  <- Total acc sans value on PeriodEnd
        //   accMemo:          8           43 (51-8)   21         7
        //    newAcc:                      55 (43+12)  
        //       acc: 1  3  8  26  41  51  55(51+12-8) 28(64+7-43) 7(28-21)    
        //                                      64                      10  16  9
        //Temps(min): 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15  
        //     Value: 1  2  5  18  15  10  12  9       7              3   6   8
        //   Pub Acc:          8           51          64         28          16  
        // Pub Count:          3           6           5          3           3  

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 2, T._1));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

        var now = publicationPeriod;
        sut.Update(now);
        var outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 8f, 0, 3),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 0, 3)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 18, T._3));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 15, T._4));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 10, T._5));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 12, T._6));

        now = publicationPeriod * 2;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 51f, 0, 6),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 6f, 0, 6)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 9, T._7));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 7, T._9));

        now = publicationPeriod * 3;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 64f, 3, 9),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 5f, 3, 9)
        );

        now = publicationPeriod * 4;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 28, 6, 12),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 6, 12)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 3, T._13));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 6, T._14));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._15));

        now = publicationPeriod * 5;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 16f, 9, 15),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 9, 15)
        );
    }

    [Test]
    [Ignore("Currently, there is no use case with a Window period < Metric publication period")]
    public void GivenWindowSmallerThanMetricPeriod()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:windowed:accumulator");
        var publicationPeriod = T._6;

        var sut = CreateSut(outputUrn, publicationPeriod, publicationPeriod / 3, TimeSpan.Zero);

        //Temps(min): 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15  16  17  18
        //     Value: 1  2  5  18  15  10  12  9       7                      3   6       8
        //   Pub Acc:                      25                     0                       6
        // Pub Count:                      2                      0                       1

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 2, T._1));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 18, T._3));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 15, T._4));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 10, T._5));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 12, T._6));

        var now = publicationPeriod;
        sut.Update(now);
        var outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 25f, 4, 6),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 2f, 4, 6)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 9, T._7));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 7, T._9));

        now = publicationPeriod * 2;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 0f, 10, 12),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 0f, 10, 12)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 3, T._15));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 6, T._16));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._18));

        now = publicationPeriod * 3;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 6f, 16, 18),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 1f, 16, 18)
        );
    }

    [Test]
    [Ignore("Currently, that can be a limitation allowed to simply implementation")]
    public void GivenWindowPeriodIsNotMultipleOfMetricPeriod()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:windowed:accumulator");
        var publicationPeriod = T._3;
        var windowPeriod = T._4;

        var sut = CreateSut(outputUrn, publicationPeriod, windowPeriod, TimeSpan.Zero);

        //       acc:  1  3  8  26  41  51  (51-3)
        //Temps(min): 0  1  2  3   4   5   6   7   8   9  10  11  12  13  14  15  
        //     Value: 1  2  5  18  15  10  12  9       7              3   6   8
        //   Pub Acc:          8           48          31         7           9
        // Pub Count:          3           4           3          1           2

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 1, T._0));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 2, T._1));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 5, T._2));

        var now = publicationPeriod;
        sut.Update(now);
        var outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 8f, 0, 3),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 0, 3)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 18, T._3));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 15, T._4));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 10, T._5));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 12, T._6));

        now = publicationPeriod * 2;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 48f, 2, 6),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 4f, 2, 6)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 9, T._7));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 7, T._9));

        now = publicationPeriod * 3;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 31f, 5, 9),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 5, 9)
        );

        now = publicationPeriod * 4;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 28, 6, 12),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 6, 12)
        );

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 3, T._13));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 6, T._14));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 8, T._15));

        now = publicationPeriod * 5;
        sut.Update(now);
        outcome = sut.Publish(now).GetValue();
        Check.That((Property<MetricValue>[]) outcome).ContainsExactly(
            PDH.CreateAccumulatorValue(new[] {outputUrn.Value}, 16f, 9, 15),
            PDH.CreateAccumulatorCount(new[] {outputUrn.Value}, 3f, 9, 15)
        );
    }

    [Test]
    public void should_reset_values_at_publish()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:accumulator");
        var publicationPeriod = TimeSpan.FromMinutes(1);
        var periodStartAt = TimeSpan.Zero;

        var sut = CreateSut(outputUrn, publicationPeriod, null, TimeSpan.Zero);

        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 42f, periodStartAt));

        var firstPublishAt = publicationPeriod;
        sut.Update(firstPublishAt);
        var _ = sut.Publish(firstPublishAt);

        var secondPublishAt = publicationPeriod * 2;
        sut.Update(secondPublishAt);
        var result = sut.Publish(secondPublishAt);
        var expected = new IDataModelValue[]
        {
            Property<MetricValue>.Create(MetricUrn.BuildAccumulatedValue(outputUrn), new MetricValue(0, firstPublishAt, secondPublishAt), secondPublishAt),
            Property<MetricValue>.Create(MetricUrn.BuildSamplesCount(outputUrn), new MetricValue(0, firstPublishAt, secondPublishAt), secondPublishAt)
        };

        Check.That(result.GetValue()).IsEqualTo(expected);
    }

    [Test]
    public void should_send_accumulator()
    {
        const string inputUrn = "foo:measure";
        var outputUrn = MetricUrn.Build("foo:accumulator");
        var publicationPeriod = TimeSpan.FromMinutes(1);

        var sut = CreateSut(outputUrn, publicationPeriod, null, TimeSpan.Zero);

        //T(seconds): 0   1   2   60  61  120
        //    values: 42  42  42  42  42
        //       acc: 42  84  126 42  84
        //     count: 1   2   3
        //   Pub acc:             126     84
        // Pub count:             3       2

        var now = TimeSpan.Zero;
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 42f, now));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 42f, now + TimeSpan.FromSeconds(1)));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 42f, now + TimeSpan.FromSeconds(2)));

        now = publicationPeriod;
        sut.Update(now);
        var periodStartAt = now - publicationPeriod;
        var expected = new IDataModelValue[]
        {
            Property<MetricValue>.Create(MetricUrn.BuildAccumulatedValue(outputUrn), new MetricValue(126, periodStartAt, now), now),
            Property<MetricValue>.Create(MetricUrn.BuildSamplesCount(outputUrn), new MetricValue(3, periodStartAt, now), now)
        };

        Check.That(sut.Publish(now).GetValue()).IsEqualTo(expected);

        now += TimeSpan.FromSeconds(30);
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 42f, now));
        sut.Update(PDH.CreateDataModelFloatValue(inputUrn, 42f, now + TimeSpan.FromSeconds(1)));

        now = publicationPeriod * 2;
        sut.Update(now);
        periodStartAt = now - publicationPeriod;
        expected = new IDataModelValue[]
        {
            Property<MetricValue>.Create(MetricUrn.BuildAccumulatedValue(outputUrn), new MetricValue(84, periodStartAt, now), now),
            Property<MetricValue>.Create(MetricUrn.BuildSamplesCount(outputUrn), new MetricValue(2, periodStartAt, now), now)
        };

        var fromPublish = sut.Publish(now).GetValue();
        Check.That(fromPublish).IsEqualTo(expected);
    }

    private AccumulatorComputer CreateSut(string outputUrn, TimeSpan publicationPeriod, TimeSpan? windowPeriod, TimeSpan now)
        => new (MetricUrn.Build(outputUrn), publicationPeriod, windowPeriod, _tsReader, _tsWriter, now);
}