#nullable enable
using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public record VariationInfo(
  MetricUrn RootUrn,
  Urn InputUrn,
  TimeSpan PublicationPeriod,
  GroupInfo[] Groups,
  Option<TimeSpan> WindowRetention,
  Option<TimeSpan> StorageRetention
) : IMetricWindowableInfo
{
  public void Accept(IMetricInfoVisitor visitor)
  {
    foreach (var groupInfo in Groups) groupInfo.Accept(visitor);
    visitor.VisitVariationInfo(this);
  }
}