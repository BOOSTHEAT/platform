#nullable enable
using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public record AccumulatorInfo(
  MetricUrn RootUrn,
  Urn InputUrn,
  TimeSpan PublicationPeriod,
  GroupInfoAccumulator[] Groups,
  Option<TimeSpan> WindowRetention,
  Option<TimeSpan> StorageRetention) : AccumulatorInfoDataItem(RootUrn), IMetricWindowableInfo
{
  public void Accept(IMetricInfoVisitor visitor)
  {
    visitor.VisitAccumulatorInfo(this);
    foreach (var groupInfo in Groups) groupInfo.Accept(visitor);
  }
}

public record AccumulatorInfoDataItem(MetricUrn RootUrn)
{
  public MetricUrn AccumulatedValue { get; } = MetricUrn.BuildAccumulatedValue(RootUrn);
  public MetricUrn SamplesCount { get; } = MetricUrn.BuildSamplesCount(RootUrn);
}

public record GroupInfoAccumulator(MetricUrn RootUrn, TimeSpan PublicationPeriod, Option<TimeSpan> StorageRetention)
  : AccumulatorInfoDataItem(RootUrn), IGroupInfo
{
  public void Accept(IMetricInfoVisitor visitor) => visitor.VisitGroupInfoAccumulator(this);
}