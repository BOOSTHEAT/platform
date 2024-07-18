#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public static class CreateMetricInfos
{
  public static MetricInfoSet Execute(
    IEnumerable<Metric<MetricUrn>> metrics,
    IEnumerable<ISubSystemDefinition> stateMachines
    )
  {
    var infoSet = new MetricInfoSet();

    var stateTypes = stateMachines
      .ToDictionary(s => (Urn)s.StateUrn, s => s.StateType);
    
    foreach (var metric in metrics)
      CreateMetricInfoAndAddItInInfoSet(metric, infoSet, stateTypes);

    return infoSet;
  }

  private static void CreateMetricInfoAndAddItInInfoSet(
    Metric<MetricUrn> metric,
    MetricInfoSet infoSet,
    Dictionary<Urn, Type> stateTypes
    )
  {
    switch (metric.Kind)
    {
      case MetricKind.Gauge:
        infoSet.Add(MetricInfoFactory.CreateGaugeInfo(metric));
        break;
      case MetricKind.SampleAccumulator:
        infoSet.Add(MetricInfoFactory.CreateAccumulatorInfo(metric));
        break;
      case MetricKind.Variation:
        infoSet.Add(MetricInfoFactory.CreateVariationInfo(metric));
        break;
      case MetricKind.State:
        infoSet.Add(MetricInfoFactory.CreateStateMonitoringInfo(metric,stateTypes));
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(metric.Kind));
    }
  }
}