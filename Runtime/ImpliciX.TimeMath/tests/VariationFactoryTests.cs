using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Computers.Variation;
using NFluent;
using NUnit.Framework;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;

namespace ImpliciX.TimeMath.Tests;

public class VariationFactoryTests
{
  private ITimeMathReader _timeMathReader;
  private ITimeMathWriter _timeMathWriter;
  private readonly TimeHelper T = TimeHelper.Minutes();
  private readonly Urn _inputUrn = MetricUrn.Build("myOutputUrn");
  private readonly MetricUrn _outputUrn = MetricUrn.Build("myOutputUrn");

  [SetUp]
  public void Init()
  {
    var tmpComputersDbFolder = Path.Combine(Path.GetTempPath(), nameof(VariationFactoryTests));

    if (Directory.Exists(tmpComputersDbFolder))
      Directory.Delete(tmpComputersDbFolder, true);

    var tsDb = new TimeSeriesDb(tmpComputersDbFolder, "computers");
    _timeMathReader = new TimeBasedTimeMathReader(tsDb);
    _timeMathWriter = new TimeBasedTimeMathWriter(tsDb);
  }

  [Test]
  public void GivenVariationMetric_WhenICreate()
  {
    var metric = MFactory.CreateVariationMetric(_outputUrn, _inputUrn, 5).Builder.Build<Metric<MetricUrn>>();
    var info = MetricInfoFactory.CreateVariationInfo(metric);

    var computerInfos = CreateSut().Create(info, T._7);
    Check.That(computerInfos).HasSize(1);
    var computerInfo = computerInfos.First();

    Check.That(computerInfo.Computer).IsInstanceOf<VariationComputer>();
    Check.That(computerInfo.PublicationPeriod).IsEqualTo(info.PublicationPeriod);
    Check.That(computerInfo.TriggerUrns).IsEqualTo(new[] {info.InputUrn});
    Check.That(computerInfo.Computer.RootUrn).IsEqualTo(info.RootUrn);
  }

  [Test]
  public void GivenVariationWindowedMetric_WhenICreate()
  {
    var metric = MetricsDSL.Metric(_outputUrn)
      .Is
      .Every(5).Minutes
      .OnAWindowOf(15).Minutes
      .VariationOf(_inputUrn)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateVariationInfo(metric);

    var computerInfos = CreateSut().Create(info, T._7);
    Check.That(computerInfos).HasSize(1);
    var computerInfo = computerInfos.First();

    Check.That(computerInfo.Computer).IsInstanceOf<VariationComputerWindowed>();
    Check.That(computerInfo.PublicationPeriod).IsEqualTo(info.PublicationPeriod);
    Check.That(computerInfo.TriggerUrns).IsEqualTo(new[] {info.InputUrn});
    Check.That(computerInfo.Computer.RootUrn).IsEqualTo(info.RootUrn);
  }

  [Test]
  public void GivenVariationWithGroups_WhenICreate()
  {
    var metric = MFactory.CreateVariationMetric(_outputUrn, _inputUrn, 2)
      .Group.Hourly
      .Group.Every(7).Days
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateVariationInfo(metric);
    var computerInfos = CreateSut().Create(info, T._7);
    Check.That(computerInfos).HasSize(3);

    Check.That(computerInfos[0].PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(2));
    Check.That(computerInfos[0].TriggerUrns).IsEqualTo(new[] {_inputUrn});
    Check.That(computerInfos[0].Computer.RootUrn).IsEqualTo(_outputUrn);

    Check.That(computerInfos[1].PublicationPeriod).IsEqualTo(TimeSpan.FromHours(1));
    Check.That(computerInfos[1].TriggerUrns).IsEqualTo(new[] {_inputUrn});
    Check.That(computerInfos[1].Computer.RootUrn).IsEqualTo(MetricUrn.Build(_outputUrn, "_1Hours"));

    Check.That(computerInfos[2].PublicationPeriod).IsEqualTo(TimeSpan.FromDays(7));
    Check.That(computerInfos[2].TriggerUrns).IsEqualTo(new[] {_inputUrn});
    Check.That(computerInfos[2].Computer.RootUrn).IsEqualTo(MetricUrn.Build(_outputUrn, "_7Days"));
  }

  private TimeMathComputerFactory CreateSut() => new (_timeMathWriter, _timeMathReader);
}