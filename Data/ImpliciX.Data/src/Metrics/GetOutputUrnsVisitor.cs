using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public interface IMetricInfoVisitor
{
  void VisitGaugeInfo(GaugeInfo gauge);
  void VisitAccumulatorInfo(AccumulatorInfo accumulator);
  void VisitVariationInfo(VariationInfo variation);
  void VisitStateMonitoringInfo(StateMonitoringInfo stateMonitoring);
  void VisitGroupInfo(GroupInfo group);
  void VisitGroupInfoAccumulator(GroupInfoAccumulator group);
  void VisitGroupInfoStateMonitoring(GroupInfoStateMonitoring groupInfoStateMonitoring);
}

internal class GetOutputUrnsVisitor : IMetricInfoVisitor
{
  private readonly List<MetricUrn> _metricUrns = new ();
  public IEnumerable<MetricUrn> GetResult() => _metricUrns.AsReadOnly();

  public void VisitGaugeInfo(GaugeInfo gauge)
  {
    _metricUrns.Add(gauge.RootUrn);
  }

  public void VisitAccumulatorInfo(AccumulatorInfo accumulator)
  {
    _metricUrns.Add(accumulator.AccumulatedValue);
    _metricUrns.Add(accumulator.SamplesCount);
  }

  public void VisitVariationInfo(VariationInfo variation)
  {
    _metricUrns.Add(variation.RootUrn);
  }

  public void VisitStateMonitoringInfo(StateMonitoringInfo stateMonitoring)
  {
    VisitStateInfoNode(stateMonitoring.States);
  }

  private void VisitStateInfoNode(Dictionary<Enum, StateInfoDataItem> stateData)
  {
    foreach (var stateValue in stateData.Select(o => o.Value))
    {
      _metricUrns.Add(stateValue.Occurrence);
      _metricUrns.Add(stateValue.Duration);
      _metricUrns.AddRange(stateValue.Accumulators.SelectMany(acc => new[] {acc.AccumulatedValue, acc.SamplesCount}));
      _metricUrns.AddRange(stateValue.Variations.Select(variation => variation.OutputUrn));
    }
  }

  public void VisitGroupInfo(GroupInfo group)
  {
    _metricUrns.Add(group.RootUrn);
  }

  public void VisitGroupInfoAccumulator(GroupInfoAccumulator group)
  {
    _metricUrns.Add(group.AccumulatedValue);
    _metricUrns.Add(group.SamplesCount);
  }

  public void VisitGroupInfoStateMonitoring(GroupInfoStateMonitoring group)
  {
    VisitStateInfoNode(group.States);
  }
}