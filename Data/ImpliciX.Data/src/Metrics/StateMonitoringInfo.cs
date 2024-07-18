#nullable enable
using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public record StateInfoDataItem(MetricUrn RootUrn, StateVariationInfo[] Variations, StateAccumulatorInfo[] Accumulators)
{
  public MetricUrn Occurrence { get; } = MetricUrn.BuildOccurence(RootUrn);
  public MetricUrn Duration { get; } = MetricUrn.BuildDuration(RootUrn);
}

public record StateMonitoringInfo(
  MetricUrn RootUrn,
  Urn InputUrn,
  TimeSpan PublicationPeriod,
  Dictionary<Enum, StateInfoDataItem> States,
  GroupInfoStateMonitoring[] Groups,
  Option<TimeSpan> WindowRetention,
  Option<TimeSpan> StorageRetention
) : IMetricWindowableInfo
{
  public IGroupInfo[] GroupInfos => Groups;

  public void Accept(IMetricInfoVisitor visitor)
  {
    visitor.VisitStateMonitoringInfo(this);
    foreach (var groupInfo in Groups) groupInfo.Accept(visitor);
  }
}

public record GroupInfoStateMonitoring(
  MetricUrn RootUrn,
  Dictionary<Enum, StateInfoDataItem> States,
  TimeSpan PublicationPeriod,
  Option<TimeSpan> StorageRetention
) : IGroupInfo
{
  public void Accept(IMetricInfoVisitor visitor) => visitor.VisitGroupInfoStateMonitoring(this);
}

public record StateVariationInfo(MetricUrn OutputUrn, Urn InputUrn);
public record StateAccumulatorInfo(MetricUrn RootUrn, Urn InputUrn) : AccumulatorInfoDataItem(RootUrn);