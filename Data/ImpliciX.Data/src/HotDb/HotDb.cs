#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotDb.Hdw;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.HotDb;

public class HotDb: IHotDb
{
    internal HotDb(string dbName, Disk dbDisk, IRam ram)
    {
        DbName = dbName;
        Disk = dbDisk;
        Ram = ram;
        Planner = new Planner(new Scribe());
    }
    internal Planner Planner { get; }
    internal IRam Ram { get; }
    public string DbName { get; }
    internal Disk Disk { get; }

    public static HotDb Create(string dbPath, string dbName)
    {
        var ram = new Ram();
        var disk = Disk.Create(dbPath, dbName);
        return new HotDb(dbName, disk, ram);
    }
    public static HotDb Load(string dbPath, string dbName, bool safeLoad = false)
    {
        var ram = new Ram();
        try
        {
            var disk = Disk.Load(dbPath, dbName);
            Scribe.ReadDb(disk, ram);
            return new HotDb(dbName, disk, ram);
        }
        catch (Exception)
        {
            if (!safeLoad) throw;
            Disk.MoveToQuarantine(dbPath);
            return Create(dbPath, dbName);
        }
    }
    public void Define(StructDef def)
    {
        Planner.DefineStructure(def)
            .Execute(Disk, Ram, new WriteContext());
    }
    public void Upsert(StructDef structDef, long pk, byte[] bytes)
    {
        
        var encodedPk = BitConverter.ToInt64(bytes[..8]);
        if(encodedPk != pk) 
            throw new Exception("pk mismatch");
        var _ = new[]
        {
            Planner.DefineStructure(structDef),
            Planner.Upsert(pk, bytes)
        }.Aggregate(new WriteContext(), (ctx, op) => op.Execute(Disk, Ram, ctx));
    }
    public int Delete(string structureName, long pk)
    {
        Log.Verbose("Delete {pk} from {key}", pk, structureName);
        var ctx =  Planner
            .Delete(structureName, pk)
            .Execute(Disk, Ram, new WriteContext());
        
        return ctx.BlocksCount;
    }
    public int Delete(string structureName, long pkMin, long pkMax)
    {
        Log.Verbose("BulkDelete at {pkMin} up to {pkMax} from {key}", pkMin, pkMax, structureName);
        var ctx = Planner
            .Delete(structureName, pkMin, pkMax)
            .Execute(Disk, Ram, new BulkDeleteContext());
        return ctx.BlocksCount;
    }
    
    public byte[] Get(string structureName, long pk)
    {
        var ctx = Planner.Get(structureName, pk).Execute(Disk, Ram, new ReadSingleContext());
        return ctx?.Results ?? Array.Empty<byte>();
    }
    public Dictionary<StructDef, byte[][]> GetAll(string structureName, long? count=null, long? upTo=null)
    {
        var ctx = Planner.GetAll(structureName, count, upTo).Execute(Disk, Ram, new ReadManyContext());
        return ctx?.Results ?? new Dictionary<StructDef, byte[][]>();
    }

    public int Count(StructDef structDef)
    {
        var ctx = Planner.Count(structDef).Execute(Disk, Ram, new CountContext());
        return ctx?.Result ?? 0;
    }

    public bool IsDefined(string structureName) => Ram.TryGetStructure(structureName, out _);
    public byte[] GetFirst(string structureName, long? pkMin = null)
    {
        var ctx = Planner.GetFirst(structureName, pkMin).Execute(Disk, Ram, new ReadSingleContext());
        return ctx?.Results ?? Array.Empty<byte>();
    }

    public StructDef[] DefinedStructs => Ram.AllStructureDefinitions();

    public byte[] GetLast(string structureName, long? pkMax=null)
    {
        var ctx = Planner.GetLast(structureName, pkMax).Execute(Disk, Ram, new ReadSingleContext());
        return ctx?.Results ?? Array.Empty<byte>();
    }

    public IReadOnlyDictionary<string,HashSet<StructDef>> DefinedStructsByName => Ram.AllStructuresByName;

    public void Dispose()
    {
        if(IsDisposed) return;
        Log.Information("[HotDb - {name}] Force flush before disposing.", DbName);
        Disk.ForceFlush();
        Disk.Dispose();
        Log.Information("[HotDb - {name}] Disposed", DbName);
        IsDisposed = true;
    }
    private bool IsDisposed { get; set; }
}