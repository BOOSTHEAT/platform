using System;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using NFluent;
using NUnit.Framework;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.Data.Tests.Metrics;

public class FactoryVariationInfoTests
{
  private readonly Urn _inputUrn = Urn.BuildUrn("foo:inputUrn");
  private readonly MetricUrn _outputUrn = MetricUrn.Build("foo:outputUrn");

  [Test]
  public void GivenWrongMetricKind_WhenICreate_ThenIGetAnError()
  {
    var metric = MFactory
      .CreateGaugeMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var ex = Check.ThatCode(() => MetricInfoFactory.CreateVariationInfo(metric))
      .Throws<InvalidOperationException>()
      .Value;

    Check.That(ex.Message).Contains("Metric kind must be 'Variation', but this metric has kind is 'Gauge'");
  }

  [Test]
  public void GivenSimpleOne_WhenICreate()
  {
    var metric = MFactory
      .CreateVariationMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateVariationInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(5));
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.Groups).IsEmpty();
  }

  [Test]
  public void GivenWithGroups_WhenICreate()
  {
    var inputUrn = Urn.BuildUrn("foo:inputUrn");
    var outputUrn = MetricUrn.Build("foo:outputUrn");

    var metric = MFactory
      .CreateVariationMetric(outputUrn, inputUrn, 5)
      .Group.Every(30).Days
      .Group.Every(18).Minutes
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateVariationInfo(metric);
    // @formatter:off
    var expected = new (object Challenger, object ExpectedValue)[]
    {
      (info.PublicationPeriod,          TimeSpan.FromMinutes(5)),
      (info.InputUrn,                   "foo:inputUrn"),
      (info.RootUrn,                    "foo:outputUrn"),
      (info.Groups.Length,               2),
      (info.Groups[0].PublicationPeriod, TimeSpan.FromDays(30)),
      (info.Groups[0].RootUrn,           "foo:outputUrn:_30Days"),
      (info.Groups[1].PublicationPeriod, TimeSpan.FromMinutes(18)),
      (info.Groups[1].RootUrn,           "foo:outputUrn:_18Minutes")
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
      .VariationOf(_inputUrn)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateVariationInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(3));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
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
      .VariationOf(_inputUrn)
      .Over.ThePast(15).Days
      .Group.Daily.Over.ThePast(30).Days
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateVariationInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(3));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(metric.TargetUrn);
    Check.That(info.StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromDays(15));
    Check.That(info.WindowRetention.IsSome).IsFalse();
    Check.That(info.Groups).HasSize(1);
    Check.That(info.Groups[0].StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromDays(30));
  }
}