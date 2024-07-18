#nullable enable
using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public record GaugeInfo(
  MetricUrn RootUrn,
  Urn InputUrn,
  TimeSpan PublicationPeriod,
  GroupInfo[] Groups,
  Option<TimeSpan> StorageRetention
) : IMetricInfo
{
  public void Accept(IMetricInfoVisitor visitor)
  {
    foreach (var groupInfo in Groups) groupInfo.Accept(visitor);
    visitor.VisitGaugeInfo(this);
  }
}