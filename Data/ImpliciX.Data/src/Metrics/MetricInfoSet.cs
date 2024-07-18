#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public class MetricInfoSet
{
  private readonly List<IMetricInfo> _metricInfos = new ();

  public IEnumerable<IMetricInfo> GetMetricInfos() => _metricInfos.AsReadOnly();
  public IEnumerable<TMetricInfo> GetMetricInfos<TMetricInfo>() => _metricInfos.OfType<TMetricInfo>();
  internal void Add(IMetricInfo metricInfo) => _metricInfos.Add(metricInfo);

  public IEnumerable<MetricUrn> GetOutputUrns()
  {
    var getOutputUrnsVisitor = new GetOutputUrnsVisitor();
    Accept(getOutputUrnsVisitor);
    return getOutputUrnsVisitor.GetResult();
  }

  public void Accept(IMetricInfoVisitor visitor)
  {
    foreach (var metricInfo in _metricInfos) metricInfo.Accept(visitor);
  }
}

public record GroupInfo(MetricUrn RootUrn, TimeSpan PublicationPeriod, Option<TimeSpan> StorageRetention) : IGroupInfo
{
  public void Accept(IMetricInfoVisitor visitor) => visitor.VisitGroupInfo(this);
}