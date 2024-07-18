using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries.Storage;

internal sealed class TimeSeriesDbRepository : IMetricsDbRepository
{
  private readonly TimeSeriesDb _db;

  public TimeSeriesDbRepository(
    IDefinedSeries definedSeries,
    string folderPath,
    string dbName,
    bool safeLoad = false
  )
  {
    _db = new TimeSeriesDb(
      folderPath,
      dbName,
      safeLoad
    );
    foreach (var rootUrn in definedSeries.RootUrns)
    {
      var (storableProperties, retention) = definedSeries.StorablePropertiesForRoot(rootUrn);
      foreach (var urn in storableProperties)
        _db.SetupTimeSeries(
          urn,
          retention
        );
    }
  }

  public IEnumerable<DataModelValue<MetricValue>> Read(
    Urn rootUrn,
    Urn[]? properties = null,
    TimeSpan? from = null,
    TimeSpan? to = null
  )
  {
    var selectedUrn = properties?.FirstOrDefault() ?? rootUrn;
    return _db.ReadAll(selectedUrn).Match(
      Array.Empty<DataModelValue<MetricValue>>,
      values =>
        values
          .Select(
            o => new DataModelValue<MetricValue>(
              o.Urn,
              new MetricValue(
                o.Value,
                TimeSpan.Zero,
                TimeSpan.Zero
              ), o.At
            )
          )
          .ToArray()
    );
  }

  public void WriteMany(
    Urn rootUrn,
    DataModelValue<MetricValue>[] metricValues
  )
  {
    foreach (var point in metricValues)
      _db.Write(
        point.Urn,
        point.At,
        point.Value.Value
      );
  }

  public void ApplyRetentionPolicy()
  {
    _db.ApplyRetentionPolicy();
  }

  public void Dispose()
  {
    _db.Dispose();
  }
}
