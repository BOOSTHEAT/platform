using System;
using System.Collections.Generic;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Data.Tools;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Records.ColdRecords;

public interface IColdRecordsDb
{
  void Write(Snapshot snapshot);
}

public class ColdRecordsDb : ColdDb<RecordsDataPoint>, IColdRecordsDb
{
  public const string FileExtension = ".records";

  private ColdRecordsDb(
    string storageFolderPath,
    Dictionary<Urn, IColdCollection<RecordsDataPoint>> collections,
    IRotateFilePolicy<RecordsDataPoint> rotateFilePolicy) : base(storageFolderPath, collections, rotateFilePolicy)
  {
  }

  public void Write(Snapshot snapshot)
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(ColdMetricsDb));
    var dataPoints = RecordsDataPoint.FromSnapshot(snapshot);
    WriteMany(snapshot.RecordUrn, new[] {dataPoints});
  }

  public static ColdRecordsDb LoadOrCreate(
    Urn[] collectionUrns,
    string folderPath,
    bool safeLoad = false,
    IRotateFilePolicy<RecordsDataPoint>? rotatePolicy = null)
  {
    var coldFiles = LoadOrCreate(
      collectionUrns,
      folderPath,
      1,
      safeLoad,
      FileExtension,
      NewCollection,
      LoadCollection);

    return new ColdRecordsDb(folderPath, coldFiles, rotatePolicy ?? new OneCompressedFileByDay<RecordsDataPoint>());
  }

  public static ColdCollection<RecordsDataPoint> NewCollection(
    string storageFolderPath,
    Urn urn,
    byte protocolVersion = 1
  ) =>
    new(
      storageFolderPath,
      urn,
      ProtocolFactory.Create(protocolVersion)
    );

  public static ColdCollection<RecordsDataPoint> LoadCollection(string filePath) =>
    new(filePath, ProtocolFactory.Create);

  public static ColdCollectionReader<RecordsDataPoint> LoadCollectionFromStore(IStoreFile storeFile) =>
    new(storeFile, ProtocolFactory.Create);

  public static ColdReader<RecordsDataPoint> CreateReader(string folderPath, bool safeLoad = false) =>
    new(folderPath, FileExtension, LoadCollection);

  public static ColdReader<RecordsDataPoint> CreateReader(IStoreFolder folderPath, bool safeLoad = false) =>
    new(folderPath, FileExtension, LoadCollectionFromStore);
}
