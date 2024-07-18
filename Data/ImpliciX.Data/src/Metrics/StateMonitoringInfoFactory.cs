#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public static class StateMonitoringInfoFactory
{
  public static StateMonitoringInfo CreateStateMonitoring(
    Metric<MetricUrn> metric,
    Dictionary<Urn, Type> stateTypes
    )
  {
    MetricInfoFactory.AssumeMetricKindIsAsExpected(metric, MetricKind.State);

    var inputValueType = metric.InputType.GetValue();
    if (inputValueType is null)
      throw new InvalidOperationException($"Cannot get state type to build {nameof(StateMonitoringInfoFactory)} for urn {metric.InputUrn}");

    if (stateTypes.TryGetValue(metric.InputUrn, out var type))
    {
      Log.Debug("StateMonitoring info for metric '{@target}' has input '{@input}' of type '{@initialType}' / '{@type}'",
        metric.TargetUrn.Value, metric.InputUrn.Value, inputValueType.FullName, type.FullName);
      inputValueType = type;
    }
    else
    {
      Log.Debug("StateMonitoring info for metric '{@target}' has input '{@input}' of type '{@type}'",
        metric.TargetUrn.Value, metric.InputUrn.Value, inputValueType.FullName);
    }

    return new StateMonitoringInfo(
      MetricUrn.Build(metric.TargetUrn),
      metric.InputUrn,
      metric.PublicationPeriod,
      CreateDataItemsForEachState(metric.TargetUrn, inputValueType, metric.SubMetricDefs.ToArray()),
      CreateGroupsFromMetric(metric, inputValueType),
      MetricInfoFactory.ToTimeSpan(metric.WindowPolicy),
      MetricInfoFactory.ToTimeSpan(metric.StoragePolicy)
    );
  }

  private static GroupInfoStateMonitoring[] CreateGroupsFromMetric(Metric<MetricUrn> metric, Type inputValueType)
    => metric.GroupPolicies
      .Select(gPolicy => CreateGroupInfo(metric.TargetUrn, metric.SubMetricDefs.ToArray(), inputValueType, gPolicy))
      .ToArray();

  private static GroupInfoStateMonitoring CreateGroupInfo(Urn rootUrn, SubMetricDef[] subMetricDefs, Type stateType, GroupPolicy groupPolicy)
  {
    var groupRootUrn = MetricUrn.Build(rootUrn, groupPolicy.Name);
    var dataItemsPerState = CreateDataItemsForEachState(groupRootUrn, stateType, subMetricDefs);
    var storageRetention = groupPolicy.StoragePolicy.Map(gp => gp.ToTimeSpan());
    return new GroupInfoStateMonitoring(groupRootUrn, dataItemsPerState, groupPolicy.Period, storageRetention);
  }

  private static Dictionary<Enum, StateInfoDataItem> CreateDataItemsForEachState(
    Urn rootUrn,
    Type stateType,
    SubMetricDef[] subMetricDefs)
  {
    var stateEnumValues = Enum.GetValues(stateType).Cast<Enum>().ToArray();
    return stateEnumValues.ToDictionary(
      state => state,
      state =>
      {
        var stateRootUrn = MetricUrn.Build(rootUrn, state.ToString());
        return new StateInfoDataItem(
          stateRootUrn,
          CreateIncludedVariations(stateRootUrn, subMetricDefs),
          CreateIncludedAccumulators(stateRootUrn, subMetricDefs)
        );
      });
  }

  private static StateVariationInfo[] CreateIncludedVariations(MetricUrn rootUrn, IEnumerable<SubMetricDef> subMetricDefs)
    => subMetricDefs
      .Where(def => def.MetricKind == MetricKind.Variation)
      .Select(def => new StateVariationInfo(MetricUrn.Build(rootUrn, def.SubMetricName), def.InputUrn)
      ).ToArray();

  private static StateAccumulatorInfo[] CreateIncludedAccumulators(MetricUrn rootUrn, IEnumerable<SubMetricDef> subMetricDefs)
    => subMetricDefs
      .Where(def => def.MetricKind == MetricKind.SampleAccumulator)
      .Select(def => new StateAccumulatorInfo(MetricUrn.Build(rootUrn, def.SubMetricName), def.InputUrn)
      ).ToArray();
}