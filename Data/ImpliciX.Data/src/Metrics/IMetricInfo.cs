#nullable enable
using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public interface IInfoBase
{
  MetricUrn RootUrn { get; }
  TimeSpan PublicationPeriod { get; }
  Option<TimeSpan> StorageRetention { get; }
  void Accept(IMetricInfoVisitor visitor);
}

public interface IMetricInfo : IInfoBase
{
}

public interface IMetricWindowableInfo : IMetricInfo
{
  Option<TimeSpan> WindowRetention { get; }
}

public interface IGroupInfo : IInfoBase
{
}

public static class IMetricInfoBaseExtensions
{
  public static IEnumerable<MetricUrn> GetOutputUrns(this IInfoBase info)
  {
    var getOutputUrnsVisitor = new GetOutputUrnsVisitor();
    info.Accept(getOutputUrnsVisitor);
    return getOutputUrnsVisitor.GetResult();
  }
}