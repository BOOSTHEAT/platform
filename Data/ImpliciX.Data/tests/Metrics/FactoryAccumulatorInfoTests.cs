using System;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using NFluent;
using NUnit.Framework;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;

namespace ImpliciX.Data.Tests.Metrics;

public class FactoryAccumulatorInfoTests
{
  private readonly Urn _inputUrn = Urn.BuildUrn("foo:inputUrn");
  private readonly MetricUrn _outputUrn = MetricUrn.Build("foo:outputUrn");


  [Test]
  public void GivenWrongMetricKind_WhenICreate_ThenIGetAnError()
  {
    var metric = MFactory
      .CreateGaugeMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var ex = Check.ThatCode(() => MetricInfoFactory.CreateAccumulatorInfo(metric))
      .Throws<InvalidOperationException>()
      .Value;

    Check.That(ex.Message).Contains("Metric kind must be 'SampleAccumulator', but this metric has kind is 'Gauge'");
  }

  [Test]
  public void GivenSimpleOne_WhenICreate()
  {
    var metric = MFactory
      .CreateAccumulatorMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(5));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(metric.TargetUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.AccumulatedValue).IsEqualTo(MetricUrn.BuildAccumulatedValue(_outputUrn));
    Check.That(info.SamplesCount).IsEqualTo(MetricUrn.BuildSamplesCount(_outputUrn));
    Check.That(info.Groups).IsEmpty();
    Check.That(info.WindowRetention.IsSome).IsFalse();
  }

  [Test]
  public void GivenWithGroups_WhenICreate()
  {
    var inputUrn = Urn.BuildUrn("foo:inputUrn");
    var outputUrn = MetricUrn.Build("foo:outputUrn");

    var metric = MFactory
      .CreateAccumulatorMetric(outputUrn, inputUrn, 5)
      .Group.Every(7).Days
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);

    // @formatter:off
    var expected = new (object Challenger, object ExpectedValue)[]
    {
      (info.PublicationPeriod,           TimeSpan.FromMinutes(5)),
      (info.InputUrn,                    "foo:inputUrn"),
      (info.RootUrn,                     "foo:outputUrn"),
      (info.AccumulatedValue,            "foo:outputUrn:accumulated_value"),
      (info.SamplesCount,                "foo:outputUrn:samples_count"),

      (info.Groups.Length, 2),
      (info.Groups[0].PublicationPeriod, TimeSpan.FromDays(7)),
      (info.Groups[0].AccumulatedValue,  "foo:outputUrn:_7Days:accumulated_value"),
      (info.Groups[0].SamplesCount,      "foo:outputUrn:_7Days:samples_count"),
      (info.Groups[1].PublicationPeriod, TimeSpan.FromMinutes(1)),
      (info.Groups[1].AccumulatedValue,  "foo:outputUrn:_1Minutes:accumulated_value"),
      (info.Groups[1].SamplesCount,      "foo:outputUrn:_1Minutes:samples_count"),

      (info.WindowRetention.IsSome,      false)
    };
    // @formatter:on

    expected.ForEach(e =>
    {
      switch (e.Challenger)
      {
        case Urn urn:
          Check.That(urn.Value).IsEqualTo(e.ExpectedValue);
          break;
        default:
          Check.That(e.Challenger).IsEqualTo(e.ExpectedValue);
          break;
      }
    });
  }

  [Test]
  public void GivenWithWindow_WhenICreate()
  {
    var metric = MetricsDSL.Metric(_outputUrn)
      .Is
      .Every(3).Minutes
      .OnAWindowOf(15).Minutes
      .AccumulatorOf(_inputUrn)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(3));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(metric.TargetUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.AccumulatedValue).IsEqualTo(MetricUrn.BuildAccumulatedValue(_outputUrn));
    Check.That(info.SamplesCount).IsEqualTo(MetricUrn.BuildSamplesCount(_outputUrn));
    Check.That(info.Groups).IsEmpty();
    Check.That(info.WindowRetention.IsSome).IsTrue();
    Check.That(info.WindowRetention.GetValue()).IsEqualTo(TimeSpan.FromMinutes(15));
  }

  [Test]
  public void GivenWithStorageRetention_WhenICreate()
  {
    var metric = MetricsDSL.Metric(_outputUrn)
      .Is
      .Every(3).Minutes
      .AccumulatorOf(_inputUrn)
      .Over.ThePast(15).Days
      .Group.Daily.Over.ThePast(30).Days
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateAccumulatorInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(3));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(metric.TargetUrn);
    Check.That(info.StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromDays(15));
    Check.That(info.AccumulatedValue).IsEqualTo(MetricUrn.BuildAccumulatedValue(_outputUrn));
    Check.That(info.SamplesCount).IsEqualTo(MetricUrn.BuildSamplesCount(_outputUrn));
    Check.That(info.WindowRetention.IsSome).IsFalse();
    Check.That(info.Groups).HasSize(1);
    Check.That(info.Groups[0].StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromDays(30));
  }
}