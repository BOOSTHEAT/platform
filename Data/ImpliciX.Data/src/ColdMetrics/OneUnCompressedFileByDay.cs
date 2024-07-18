#nullable enable
using System;
using System.IO;
using ImpliciX.Data.ColdDb;

namespace ImpliciX.Data.ColdMetrics;

public class OneUnCompressedFileByDay : IRotateFilePolicy<MetricsDataPoint>
{
    public bool ShouldRotate(TimeSpan firstDataPointTime, TimeSpan at, long count)
    {
        if (count == 0) return false;
        var shouldRotate = new DateTime(firstDataPointTime.Ticks, DateTimeKind.Utc).Day != new DateTime(at.Ticks, DateTimeKind.Utc).Day;
        return shouldRotate;
    }

    public IColdCollection<MetricsDataPoint> Rotate(IColdCollection<MetricsDataPoint> finishedCollection, string storageFolderPath, string finishedFolder)
    {
        finishedCollection.Dispose();
        var finishFolder = Path.Combine(storageFolderPath, finishedFolder);
        if (!Directory.Exists(finishFolder))
            Directory.CreateDirectory(finishFolder);

        var finishedFilePath = Path.Combine(finishFolder, Path.GetFileName(finishedCollection.FilePath)!);
        File.Move(finishedCollection.FilePath!, finishedFilePath);

        var newColdMetric = finishedCollection.StartNewFile();
        OnRotate?.Invoke(newColdMetric, finishedCollection, finishedFilePath);
        return newColdMetric;
    }

    public void Shutdown(IColdCollection<MetricsDataPoint> db, string storageFolderPath, string finishedFolder)
    {
    }

    public Action<IColdCollection<MetricsDataPoint>, IColdCollection<MetricsDataPoint>, string>? OnRotate { get; set; }
}