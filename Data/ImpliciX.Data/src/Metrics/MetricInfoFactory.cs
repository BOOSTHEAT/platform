#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public static class MetricInfoFactory
{
  public static GaugeInfo CreateGaugeInfo(Metric<MetricUrn> metric)
  {
    AssumeMetricKindIsAsExpected(metric, MetricKind.Gauge);

    return new GaugeInfo(
      MetricUrn.Build(metric.TargetUrn),
      metric.InputUrn,
      metric.PublicationPeriod,
      GetGroupInfos(metric),
      ToTimeSpan(metric.StoragePolicy)
    );
  }

  public static VariationInfo CreateVariationInfo(Metric<MetricUrn> metric)
  {
    AssumeMetricKindIsAsExpected(metric, MetricKind.Variation);

    return new VariationInfo(
      MetricUrn.Build(metric.TargetUrn),
      metric.InputUrn,
      metric.PublicationPeriod,
      GetGroupInfos(metric),
      ToTimeSpan(metric.WindowPolicy),
      ToTimeSpan(metric.StoragePolicy)
    );
  }

  public static AccumulatorInfo CreateAccumulatorInfo(Metric<MetricUrn> metric)
  {
    AssumeMetricKindIsAsExpected(metric, MetricKind.SampleAccumulator);

    var groups = metric.GroupPolicies
      .Select(gPolicy =>
        new GroupInfoAccumulator(MetricUrn.Build(metric.Target, gPolicy.Name),
          gPolicy.Period,
          ToTimeSpan(gPolicy.StoragePolicy)
        )
      ).ToArray();

    return new AccumulatorInfo(
      MetricUrn.Build(metric.Target),
      metric.InputUrn,
      metric.PublicationPeriod,
      groups,
      ToTimeSpan(metric.WindowPolicy),
      ToTimeSpan(metric.StoragePolicy)
    );
  }

  public static StateMonitoringInfo CreateStateMonitoringInfo(
    Metric<MetricUrn> metric,
    Dictionary<Urn, Type> stateTypes
    )
    => StateMonitoringInfoFactory.CreateStateMonitoring(metric, stateTypes);

  internal static void AssumeMetricKindIsAsExpected(IMetric metric, MetricKind expected)
  {
    if (metric.Kind != expected)
      throw new InvalidOperationException(
        $"Metric kind must be '{expected}', but this metric has kind is '{metric.Kind}' (metric output urn={metric.TargetUrn})");
  }

  internal static Option<TimeSpan> ToTimeSpan(Option<WindowPolicy> policy) => policy.Map(it => it.ToTimeSpan());
  internal static Option<TimeSpan> ToTimeSpan(Option<StoragePolicy> policy) => policy.Map(it => it.ToTimeSpan());

  private static GroupInfo[] GetGroupInfos(Metric<MetricUrn> metric)
    => metric.GroupPolicies
      .Select(gPolicy => new GroupInfo(
        MetricUrn.Build(metric.Target, gPolicy.Name),
        gPolicy.Period,
        ToTimeSpan(gPolicy.StoragePolicy))
      ).ToArray();
}