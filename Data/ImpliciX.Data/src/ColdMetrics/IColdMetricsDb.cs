using System;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdMetrics;

public interface IColdMetricsDb : IDisposable
{
  void WriteMany(Urn metricUrn, DataModelValue<MetricValue>[] series);
}

public interface IIndexedColdMetricsDb : IColdMetricsDb
{
  void ApplyRetentionPolicy();
}
