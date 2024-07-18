using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Computers;
using ImpliciX.TimeMath.Computers.StateMonitoring;
using ImpliciX.TimeMath.Computers.Variation;

namespace ImpliciX.TimeMath;

internal sealed class TimeMathComputerFactory
{
  private readonly ITimeMathReader _timeMathReader;
  private readonly ITimeMathWriter _timeMathWriter;
  private TimeSpan _startAt;

  public TimeMathComputerFactory(ITimeMathWriter timeMathWriter, ITimeMathReader timeMathReader)
  {
    _timeMathWriter = timeMathWriter ?? throw new ArgumentNullException(nameof(timeMathWriter));
    _timeMathReader = timeMathReader ?? throw new ArgumentNullException(nameof(timeMathReader));
  }

  public ComputerRuntimeInfo[] Create(IMetricInfo metricInfo, TimeSpan startAt)
  {
    if (metricInfo == null) throw new ArgumentNullException(nameof(metricInfo));
    _startAt = startAt;

    return metricInfo switch
    {
      GaugeInfo gaugeInfo => CreateGaugeRuntimeInfos(gaugeInfo),
      AccumulatorInfo accumulatorInfo => CreateAccumulatorRuntimeInfos(accumulatorInfo),
      VariationInfo variationInfo => CreateVariationRuntimeInfos(variationInfo),
      StateMonitoringInfo stateMonitoringInfo => CreateStateMonitoringRuntimeInfos(stateMonitoringInfo),
      _ => throw new ArgumentOutOfRangeException(nameof(metricInfo))
    };
  }

  private ComputerRuntimeInfo[] CreateGaugeRuntimeInfos(GaugeInfo gauge)
  {
    return new[]
    {
      CreateComputerRuntimeInfo(
        gauge.InputUrn,
        gauge.PublicationPeriod,
        new GaugeComputer(gauge.RootUrn, _timeMathWriter, _timeMathReader, _startAt)
      )
    };
  }

  private ComputerRuntimeInfo[] CreateAccumulatorRuntimeInfos(AccumulatorInfo accumulator)
  {
    return new[]
    {
      CreateComputerRuntimeInfo(
        accumulator.InputUrn,
        accumulator.PublicationPeriod,
        new AccumulatorComputer(accumulator.RootUrn, _timeMathWriter, _timeMathReader, accumulator.WindowRetention, _startAt)
      )
    };
  }

  private ComputerRuntimeInfo[] CreateStateMonitoringRuntimeInfos(StateMonitoringInfo info) =>
    new[]
    {
      CreateComputerRuntimeInfo(
        info.InputUrn,
        info.PublicationPeriod,
        new StateMonitoringComputer(info, _timeMathWriter, _timeMathReader)
      )
    };

  private ComputerRuntimeInfo[] CreateVariationRuntimeInfos(VariationInfo info)
  {
    var forGroups = info.Groups.Select(group => CreateComputerRuntimeInfo(info.InputUrn, group.PublicationPeriod, CreateComputerForGroup(group.RootUrn)));
    var root = CreateComputerRuntimeInfo(info.InputUrn, info.PublicationPeriod, CreateComputer());
    return forGroups.Prepend(root).ToArray();

    ITimeMathComputer CreateComputer() => info.WindowRetention.IsSome
      ? new VariationComputerWindowed(info.RootUrn, _timeMathWriter, _timeMathReader, info.PublicationPeriod, info.WindowRetention.GetValue(), _startAt)
      : new VariationComputer(info.RootUrn, _timeMathWriter, _timeMathReader, _startAt);

    ITimeMathComputer CreateComputerForGroup(PropertyUrn<MetricValue> groupOutputUrn) =>
      new VariationComputer(groupOutputUrn, _timeMathWriter, _timeMathReader, _startAt);
  }

  private static ComputerRuntimeInfo CreateComputerRuntimeInfo(Urn inputUrn, TimeSpan publicationPeriod, ITimeMathComputer computer)
    => new (new[] {inputUrn}, publicationPeriod, computer);

  internal struct ComputerRuntimeInfo
  {
    public TimeSpan PublicationPeriod { get; }
    public ITimeMathComputer Computer { get; }
    public IEnumerable<Urn> TriggerUrns { get; }

    public ComputerRuntimeInfo(IEnumerable<Urn> triggerUrns, TimeSpan publicationPeriod, ITimeMathComputer computer)
    {
      PublicationPeriod = publicationPeriod;
      Computer = computer ?? throw new ArgumentNullException(nameof(computer));
      TriggerUrns = triggerUrns;
    }
  }
}