#nullable enable
using System;
using System.Collections.Generic;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.Tools;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdMetrics;

public class ColdMetricsDb : ColdDb<MetricsDataPoint>, IColdMetricsDb
{
  public const string FileExtension = ".metrics";

  private ColdMetricsDb(
    string storageFolderPath,
    Dictionary<Urn, IColdCollection<MetricsDataPoint>> collections,
    IRotateFilePolicy<MetricsDataPoint> rotateFilePolicy) : base(storageFolderPath, collections, rotateFilePolicy)
  {
  }

  public void WriteMany(Urn metricUrn, DataModelValue<MetricValue>[] series)
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(ColdMetricsDb));
    var dataPoints = MetricsDataPoint.FromModel(series);

    WriteMany(metricUrn, dataPoints);
  }

  public static ColdMetricsDb LoadOrCreate(
    Urn[] collectionUrns,
    string folderPath,
    bool safeLoad = false,
    IRotateFilePolicy<MetricsDataPoint>? rotatePolicy = null)
  {
    var coldFiles = LoadOrCreate(
      collectionUrns,
      folderPath,
      1,
      safeLoad,
      FileExtension,
      NewCollection,
      LoadCollection);

    return new ColdMetricsDb(folderPath, coldFiles, rotatePolicy ?? new OneCompressedFileByDay<MetricsDataPoint>());
  }

  public static ColdCollection<MetricsDataPoint> NewCollection(
    string storageFolderPath,
    Urn urn,
    byte protocolVersion = 1) =>
    new(
      storageFolderPath,
      urn,
      ProtocolFactory.Create(protocolVersion)
    );

  public static ColdCollection<MetricsDataPoint> LoadCollection(string filePath) =>
    new(filePath, ProtocolFactory.Create);

  public static ColdCollectionReader<MetricsDataPoint> LoadCollectionFromStore(IStoreFile storeFile) =>
    new(storeFile, ProtocolFactory.Create);

  public static ColdReader<MetricsDataPoint> CreateReader(string folderPath, bool safeLoad = false) =>
    new(folderPath, FileExtension, LoadCollection);

  public static ColdReader<MetricsDataPoint> CreateReader(IStoreFolder folderPath, bool safeLoad = false) =>
    new(folderPath, FileExtension, LoadCollectionFromStore);
}
