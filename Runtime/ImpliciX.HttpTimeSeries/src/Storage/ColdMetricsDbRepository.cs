using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries.Storage;

public class ColdMetricsDbRepository : IMetricsDbRepository
{
  private readonly IndexedColdMetricsDb _db;

  public ColdMetricsDbRepository(
    IDefinedSeries definedSeries,
    string folderPath,
    bool safeLoad = false
  )
  {
    var urns = definedSeries.RootUrns;
    var storableProperties = urns.ToDictionary(
        urn => definedSeries.StorablePropertiesForRoot(urn).Item1,
        urn => definedSeries.StorablePropertiesForRoot(urn).Item2
      ).SelectMany(
        pair => pair.Key.ToDictionary(
            urn => urn,
            urn => pair.Value
          )
          .ToDictionary(
            valuePair => valuePair.Key,
            valuePair => valuePair.Value
          )
      )
      .ToDictionary(
        valuePair => valuePair.Key,
        valuePair => valuePair.Value
      );
    _db = IndexedColdMetricsDb.LoadOrCreate(
      urns,
      folderPath,
      safeLoad,
      storableProperties
    );
  }

  private bool IsDisposed { get; set; }

  public IEnumerable<DataModelValue<MetricValue>> Read(
    Urn rootUrn,
    Urn[]? properties = null,
    TimeSpan? from = null,
    TimeSpan? to = null
  )
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(ColdMetricsDbRepository));
    var query = new MetricQuery(
      from,
      to
    ).AddMetric(
      rootUrn,
      properties ?? Array.Empty<Urn>()
    );
    return  _db.ReadMany(query);
  }

  public void WriteMany(
    Urn rootUrn,
    DataModelValue<MetricValue>[] metricValues
  )
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(ColdMetricsDbRepository));
    _db.WriteMany(
      rootUrn,
      metricValues
    );
  }

  public void ApplyRetentionPolicy()
  {
    _db.ApplyRetentionPolicy();
  }

  public void Dispose()
  {
    if (IsDisposed) return;
    _db.Dispose();
    IsDisposed = true;
  }
}
