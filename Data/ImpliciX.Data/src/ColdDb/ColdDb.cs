#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdDb;

public abstract class ColdDb<TDataPoint> 
    where TDataPoint : IDataPoint
{
    protected const string QuarantineFolder = "quarantine";
    protected internal string StorageFolderPath { get; set; }
    protected IRotateFilePolicy<TDataPoint> RotateFilePolicy { get; set; }

    protected internal Dictionary<Urn, IColdCollection<TDataPoint>> Collections;

    protected ColdDb(
        string storageFolderPath, 
        Dictionary<Urn, IColdCollection<TDataPoint>> collections, 
        IRotateFilePolicy<TDataPoint> rotateFilePolicy, string finishedFolder = null)
    {
        StorageFolderPath = storageFolderPath;
        RotateFilePolicy = rotateFilePolicy;
        Collections = collections;
        FinishedFolder = finishedFolder ?? "finished";
        IsDisposed = false;
    }

    public string FinishedFolder { get; set; }

    public string[] FinishedFiles
    {
        get
        {
            var finishFolder = Path.Combine(StorageFolderPath, FinishedFolder);
            if (!Directory.Exists(finishFolder))
                return Array.Empty<string>();

            return Directory.GetFiles(finishFolder);
        }
    }
    
    public string[] CurrentFiles => Collections.Values.Select(x => x.FilePath!).ToArray();

    protected static Dictionary<Urn, IColdCollection<TDataPoint>> LoadOrCreate(
        Urn[] collectionUrns, 
        string folderPath,
        byte protocolVersion,
        bool safeLoad, 
        string fileExtension,
        Func<string, Urn, byte, ColdCollection<TDataPoint>> createColdCollection,
        Func<string, ColdCollection<TDataPoint>> loadColdCollection)
    {
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath ?? throw new ArgumentNullException(nameof(folderPath)));

        var result = new Dictionary<Urn, IColdCollection<TDataPoint>>();

        foreach (var existingFile in Directory.EnumerateFiles(folderPath))
        {
            try
            {
                var coldCollection = WithExistingFile(existingFile);
                result.Add(coldCollection.MetaData.Urn!, coldCollection);
            }
            catch (Exception)
            {
                MoveToQuarantineFolder(folderPath, existingFile);
                if (!safeLoad) throw;
            }
        }

        foreach (var urn in collectionUrns)
        {
            if (result.ContainsKey(urn))
                continue;

            var coldCollection = WithNewFile(folderPath, urn);
            result.Add(urn, coldCollection);
        }

        return result;
        
        ColdCollection<TDataPoint> WithNewFile(string storageFolderPath, Urn collectionUrn)
        {
            if (storageFolderPath == null) throw new ArgumentNullException(nameof(storageFolderPath));
            if (!Directory.Exists(storageFolderPath))
                Directory.CreateDirectory(storageFolderPath);
    
            var filePath = Path.Combine(storageFolderPath, $"{Guid.NewGuid()}{fileExtension}");
            var cmf = createColdCollection(filePath, collectionUrn, protocolVersion);
            return cmf;
        }
    
        ColdCollection<TDataPoint> WithExistingFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);
    
            return loadColdCollection(filePath);
        }
    }
    
    protected void WriteMany(Urn urn, TDataPoint[] dataPoints)
    {
        if (!Collections.TryGetValue(urn, out var collection))
            throw new InvalidOperationException($"No store found for {urn}");

        foreach (var dataPoint in dataPoints)
        {
            var firstDataPointTime = collection.MetaData.FirstDataPointTime ?? dataPoints.First().At;
            if (RotateFilePolicy.ShouldRotate(firstDataPointTime, dataPoint.At, collection.MetaData.DataPointsCount))
            {
                collection = RotateFilePolicy.Rotate(collection,StorageFolderPath, FinishedFolder);
                Collections[collection.MetaData.Urn!] = collection;
            }
            collection.WriteDataPoint(dataPoint);
        }
    }
 
   
    public void Dispose()
    {
        if (IsDisposed) return;
        foreach (var db in Collections.Values)
        {
            RotateFilePolicy.Shutdown(db,StorageFolderPath, FinishedFolder);
            db.Dispose();
        }
        IsDisposed = true;
        
    }
    public static void MoveToQuarantineFolder(string folderPath, string collectionFile)
    {
        var quarantineFolder = Path.Combine(folderPath, QuarantineFolder);
        if (!Directory.Exists(quarantineFolder))
            Directory.CreateDirectory(quarantineFolder);

        var quarantineFilePath = Path.Combine(quarantineFolder, Path.GetFileName(collectionFile));
        File.Move(collectionFile, quarantineFilePath);
    }
    protected bool IsDisposed { get; set; }
}