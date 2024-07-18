using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotDb.Model;

namespace ImpliciX.Data.HotDb.Hdw;

public class Disk : IDisposable
{
    public string DbPath { get; }
    private const string QuarantineFolderName = "quarantine"; 
    private Disk(string dbPath, Io structure, Io segments, Io blocks)
    {
        DbPath = dbPath;
        Structure = structure;
        Segments = segments;
        Blocks = blocks;
    }
    
    public static Disk Create(string dbPath, string dbName)
    {
        if (!Directory.Exists(dbPath))
            Directory.CreateDirectory(dbPath ?? throw new ArgumentNullException(nameof(dbPath)));
        
        if(Directory.EnumerateFiles(dbPath).Any())
            throw new Exception("folder should be empty");

        var dbFiles = new[] {
            Path.Combine(dbPath, $"{dbName}.structure"), 
            Path.Combine(dbPath, $"{dbName}.segments"), 
            Path.Combine(dbPath, $"{dbName}.blocks")};
        
        if(dbFiles.Any(File.Exists))
            throw new Exception($"HotDb {dbName} already exists in {dbPath}");

        return new Disk(dbPath,new Io(dbFiles[0]),new Io(dbFiles[1]),new Io(dbFiles[2]));
    }
    
    public static Disk Load(string dbPath, string dbName)
    {
        if (!Directory.Exists(dbPath))
            throw new Exception($"HotDb {dbPath} does not exist");
        
        var filesInPath = Directory.EnumerateFiles(dbPath).ToArray();
        
        if (filesInPath.Length != 3)
            throw new Exception($"HotDb {dbPath} is not a valid db");
        
        var dbFiles = new[]
        {
            filesInPath.FirstOrDefault(f => f.EndsWith(".structure")),
            filesInPath.FirstOrDefault(f => f.EndsWith(".segments")),
            filesInPath.FirstOrDefault(f => f.EndsWith(".blocks"))
        };
        
        if(dbFiles.Any(string.IsNullOrWhiteSpace))
            throw new Exception($"HotDb {dbPath} is not a valid db");
        
        return new Disk(dbPath,new Io(dbFiles[0]),new Io(dbFiles[1]),new Io(dbFiles[2]));
    }
    
    

  

    internal IIo Blocks { get; set; }

    internal IIo Segments { get; set; }

    internal IIo Structure { get; set; }
    
    public void Dispose()
    {
        if(IsDisposed) return;
        Structure.Dispose();
        Segments.Dispose();
        Blocks.Dispose();
        IsDisposed = true;
    }

    private bool IsDisposed { get; set; }

    public void Flush()
    {
        Structure.Flush();
        Segments.Flush();
        Blocks.Flush();
    }

    public void ForceFlush()
    {
        Structure.ForceFlush();
        Segments.ForceFlush();
        Blocks.ForceFlush();
    }

    public static void MoveToQuarantine(string dbPath)
    {
        var quarantineFolder = Path.Combine(dbPath, QuarantineFolderName, Guid.NewGuid().ToString());
        if (!Directory.Exists(quarantineFolder))
            Directory.CreateDirectory(quarantineFolder);
        
        var filesInPath = Directory.EnumerateFiles(dbPath).ToArray();
        foreach (var file in filesInPath)
        {
            var fileName = Path.GetFileName(file);
            var quarantineFilePath = Path.Combine(quarantineFolder, fileName);
            File.Move(file, quarantineFilePath);
        }
    }
}