#nullable enable
using System;
using System.IO;

namespace ImpliciX.Data.ColdDb;

public interface IRotateFilePolicy<TDataPoint> where TDataPoint : IDataPoint
{
  Action<IColdCollection<TDataPoint>, IColdCollection<TDataPoint>, string>? OnRotate { get; set; }
  bool ShouldRotate(TimeSpan firstDataPointTime, TimeSpan at, long count);
  IColdCollection<TDataPoint> Rotate(IColdCollection<TDataPoint> finishedCollection, string storageFolderPath,
    string finishedFolder);
  void Shutdown(IColdCollection<TDataPoint> db, string storageFolderPath, string finishedFolder);

  public static string Finish(IColdCollection<TDataPoint> finishedCollection, string storageFolderPath,
    string finishedFolder)
  {
    finishedCollection.Dispose();
    var finishFolder = Path.Combine(storageFolderPath, finishedFolder);
    if (!Directory.Exists(finishFolder))
      Directory.CreateDirectory(finishFolder);

    var finishedFilePath = Path.Combine(finishFolder, Path.GetFileName(finishedCollection.FilePath)!);
    File.Move(finishedCollection.FilePath!, finishedFilePath);
    Zip.CreateZipFromFiles(new[] {finishedFilePath}, $"{finishedFilePath}.zip");
    File.Delete(finishedFilePath);
    return finishedFilePath;
  }
}

public class OneCompressedFileByDay<TDataPoint> : IRotateFilePolicy<TDataPoint> where TDataPoint : IDataPoint
{
  public bool ShouldRotate(TimeSpan firstDataPointTime, TimeSpan at, long count)
  {
    if (count == 0) return false;
    return new DateTime(firstDataPointTime.Ticks, DateTimeKind.Utc).Day != new DateTime(at.Ticks, DateTimeKind.Utc).Day;
  }

  public IColdCollection<TDataPoint> Rotate(IColdCollection<TDataPoint> finishedCollection, string storageFolderPath,
    string finishedFolder)
  {
    var finishedFilePath = IRotateFilePolicy<TDataPoint>.Finish(finishedCollection, storageFolderPath, finishedFolder);

    var newColdMetric = finishedCollection.StartNewFile();
    OnRotate?.Invoke(newColdMetric, finishedCollection, finishedFilePath);
    return newColdMetric;
  }

  public virtual void Shutdown(IColdCollection<TDataPoint> db, string storageFolderPath, string finishedFolder)
  {
  }

  public Action<IColdCollection<TDataPoint>, IColdCollection<TDataPoint>, string>? OnRotate { get; set; }
}

public class OneCompressedFileByDayAndBySession<TDataPoint> : OneCompressedFileByDay<TDataPoint>
  where TDataPoint : IDataPoint
{
  public override void Shutdown(IColdCollection<TDataPoint> db, string storageFolderPath, string finishedFolder) =>
    IRotateFilePolicy<TDataPoint>.Finish(db, storageFolderPath, finishedFolder);
}
