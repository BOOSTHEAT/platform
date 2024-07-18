using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries.Storage;

public interface IMetricsDbRepository : IDisposable
{
  IEnumerable<DataModelValue<MetricValue>> Read(Urn rootUrn, Urn[]? properties = null, TimeSpan? from = null, TimeSpan? to = null);
  void WriteMany(Urn rootUrn, DataModelValue<MetricValue>[] metricValues);
  void ApplyRetentionPolicy();
}