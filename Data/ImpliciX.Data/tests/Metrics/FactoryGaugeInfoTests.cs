using System;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;

namespace ImpliciX.Data.Tests.Metrics;

public class FactoryGaugeInfoTests
{
  private readonly Urn _inputUrn = Urn.BuildUrn("foo:inputUrn");
  private readonly MetricUrn _outputUrn = MetricUrn.Build("foo:outputUrn");

  [Test]
  public void GivenWrongMetricKind_WhenICreate_ThenIGetAnError()
  {
    var metric = MFactory
      .CreateVariationMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var ex = Check.ThatCode(() => MetricInfoFactory.CreateGaugeInfo(metric))
      .Throws<InvalidOperationException>()
      .Value;

    Check.That(ex.Message).Contains("Metric kind must be 'Gauge', but this metric has kind is 'Variation'");
  }

  [Test]
  public void GivenSimpleOne_WhenICreate()
  {
    var metric = MFactory
      .CreateGaugeMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateGaugeInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(5));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.Groups).IsEmpty();
  }

  [Test]
  public void GivenWithGroups_WhenICreate()
  {
    var metric = MFactory
      .CreateGaugeMetric(_outputUrn, _inputUrn, 5)
      .Group.Hourly
      .Group.Every(6).Seconds
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateGaugeInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(5));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();

    Check.That(info.Groups).HasSize(2);
    Check.That(info.Groups[0].PublicationPeriod).IsEqualTo(TimeSpan.FromHours(1));
    Check.That(info.Groups[0].RootUrn).IsEqualTo(Urn.BuildUrn(_outputUrn, "_1Hours"));
    Check.That(info.Groups[0].StorageRetention.IsSome).IsFalse();
    Check.That(info.Groups[1].PublicationPeriod).IsEqualTo(TimeSpan.FromSeconds(6));
    Check.That(info.Groups[1].RootUrn).IsEqualTo(Urn.BuildUrn(_outputUrn, "_6Seconds"));
    Check.That(info.Groups[1].StorageRetention.IsSome).IsFalse();
  }

  [Test]
  public void GivenWithStorageRetention_WhenICreate()
  {
    var metric = MFactory
      .CreateGaugeMetric(_outputUrn, _inputUrn, 5)
      .Over.ThePast(10).Hours
      .Group.Hourly.Over.ThePast(20).Hours
      .Group.Every(6).Seconds.Over.ThePast(30).Hours
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateGaugeInfo(metric);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(5));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromHours(10));

    Check.That(info.Groups).HasSize(2);
    Check.That(info.Groups[0].PublicationPeriod).IsEqualTo(TimeSpan.FromHours(1));
    Check.That(info.Groups[0].RootUrn).IsEqualTo(Urn.BuildUrn(_outputUrn, "_1Hours"));
    Check.That(info.Groups[0].StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromHours(20));
    Check.That(info.Groups[1].PublicationPeriod).IsEqualTo(TimeSpan.FromSeconds(6));
    Check.That(info.Groups[1].RootUrn).IsEqualTo(Urn.BuildUrn(_outputUrn, "_6Seconds"));
    Check.That(info.Groups[1].StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromHours(30));
  }
}