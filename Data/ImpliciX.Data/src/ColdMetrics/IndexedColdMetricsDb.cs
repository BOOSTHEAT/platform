#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdMetrics;

public class IndexedColdMetricsDb : ColdDb<MetricsDataPoint>, IIndexedColdMetricsDb
{
  public const string FileExtension = ".metrics";
  private readonly Dictionary<Urn, TimeSpan>? _retentionPolicy;

  private IndexedColdMetricsDb(
    string storageFolderPath,
    Dictionary<Urn, IColdCollection<MetricsDataPoint>> collections,
    IRotateFilePolicy<MetricsDataPoint> rotateFilePolicy,
    Dictionary<Urn, TimeSpan>? retentionPolicy,
    InMemoryTimeIndex timeIndex,
    string finishedFolder
  ) :
    base(
      storageFolderPath,
      collections,
      rotateFilePolicy,
      finishedFolder
    )
  {
    TimeIndex = timeIndex;
    rotateFilePolicy.OnRotate = (
      newCollection,
      oldCollection,
      finishedFile
    ) =>
    {
      IColdCollection<MetricsDataPoint> finishedCollection = LoadCollection(finishedFile);
      TimeIndex.Replace(
        oldCollection,
        finishedCollection
      );
      TimeIndex.Add(newCollection);
    };
    _retentionPolicy = retentionPolicy;
  }

  public InMemoryTimeIndex TimeIndex { get; }
  public Dictionary<Urn, IColdCollection<MetricsDataPoint>> CurrentCollections => Collections;

  public void WriteMany(
    Urn metricUrn,
    DataModelValue<MetricValue>[] series
  )
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(ColdMetricsDb));
    var dataPoints = MetricsDataPoint.FromModel(series);

    WriteMany(
      metricUrn,
      dataPoints
    );
  }

  public void ApplyRetentionPolicy()
  {
    if (_retentionPolicy == null) return;

    if (FinishedFiles.Length == 0) return;
    var urns = Collections.Keys
        .Where(urn => _retentionPolicy.ContainsKey(urn))
        .Where(urn => _retentionPolicy[urn] != TimeSpan.Zero)
        .ToArray()
      ;
    if (urns.Length == 0) return;
    var selectedFiles = urns
        .Select(urn => Collections[urn])
        .Where(
          collection =>
            collection.MetaData.LastDataPointTime != null
        )
        .Where(
          collection =>
            collection.MetaData.Urn != null
        )
        .SelectMany(
          collection =>
            TimeIndex.CollectionsInRange(
              collection.MetaData.Urn,
              TimeSpan.MinValue,
              collection.MetaData.LastDataPointTime!.GetValueOrDefault(TimeSpan.Zero)
              - _retentionPolicy[collection.MetaData.Urn!]
              - TimeSpan.FromDays(1)
            )
        )
        .Where(
          collection =>
            FinishedFiles.Contains(collection.FilePath)
        )
        .ToDictionary(
          collection => collection.FilePath,
          collection => collection
        )
      ;
    if (selectedFiles.Keys.ToArray().Length == 0) return;
    foreach (var collection in selectedFiles.Values) collection.Dispose();
    foreach (var collection in selectedFiles.Values) TimeIndex.purge(collection);
    foreach (var selectedFile in selectedFiles.Keys) new FileInfo(selectedFile).Delete();
  }

  public static IndexedColdMetricsDb LoadOrCreate(
    Urn[] collectionUrns,
    string folderPath,
    bool safeLoad = false,
    Dictionary<Urn, TimeSpan>? retentionPolicy = null
  )
  {
    var currentCollections = LoadOrCreate(
      collectionUrns,
      folderPath,
      1,
      safeLoad,
      FileExtension,
      NewCollection,
      LoadCollection
    );

    var finishedFolder = "finished";


    var finishedPath = Path.Combine(
      folderPath,
      finishedFolder
    );
    var finishedCollections = Path.Exists(finishedPath) switch
    {
      true => Directory.EnumerateFiles(finishedPath).Select(LoadCollection).ToArray(),
      false => Array.Empty<ColdCollection<MetricsDataPoint>>()
    };

    var timeIndex = currentCollections
      .Values
      .Concat(finishedCollections)
      .Aggregate(
        new InMemoryTimeIndex(),
        (
          acc,
          collection
        ) => acc.Add(collection)
      );

    return new IndexedColdMetricsDb(
      folderPath,
      currentCollections,
      new OneUnCompressedFileByDay(),
      retentionPolicy,
      timeIndex,
      finishedFolder
    );
  }

  public static ColdCollection<MetricsDataPoint> NewCollection(
    string storageFolderPath,
    Urn urn,
    byte protocolVersion = 1
  )
  {
    return new ColdCollection<MetricsDataPoint>(
      storageFolderPath,
      urn,
      ProtocolFactory.Create(protocolVersion)
    );
  }

  public static ColdCollection<MetricsDataPoint> LoadCollection(
    string filePath
  )
  {
    return new ColdCollection<MetricsDataPoint>(
      filePath,
      ProtocolFactory.Create
    );
  }

  public IEnumerable<DataModelValue<MetricValue>> ReadMany(
    MetricQuery query
  )
  {
    return query.Metrics.SelectMany(
      s => ReadSingle(
        s.Key,
        s.Value,
        query.Start,
        query.End
      )
    );
  }

  private IEnumerable<DataModelValue<MetricValue>> ReadSingle(
    Urn metricUrn,
    Urn[] projections,
    TimeSpan from,
    TimeSpan to
  )
  {
    if (!TimeIndex.ContainsMetric(metricUrn) && !CurrentCollections.ContainsKey(metricUrn))
      return Enumerable.Empty<DataModelValue<MetricValue>>();

    var selectedCollections = TimeIndex.CollectionsInRange(
      metricUrn,
      from,
      to
    );

    var result = selectedCollections
      .SelectMany(c => c.DataPoints)
      .Where(dp => dp.At >= from && dp.At <= to)
      .OrderBy(o => o.At)
      .ToArray();

    return result.SelectMany(o => o.ToModelValues(projections.ToHashSet()));
  }
}

public class MetricQuery
{
  private readonly ConcurrentDictionary<Urn, Urn[]> _metrics = new ();

  public MetricQuery(
    TimeSpan? start = null,
    TimeSpan? end = null
  )
  {
    Start = start ?? TimeSpan.MinValue;
    End = end ?? TimeSpan.MaxValue;
  }

  public IReadOnlyDictionary<Urn, Urn[]> Metrics => _metrics;

  public TimeSpan Start { get; }
  public TimeSpan End { get; }

  public MetricQuery AddMetric(
    Urn rootUrn,
    params Urn[] properties
  )
  {
    _metrics.AddOrUpdate(
      rootUrn,
      properties,
      (
        key,
        _
      ) => properties
    );
    return this;
  }
}
