using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotDb.Hdw;
using ImpliciX.Data.HotDb.Model;
using static System.BitConverter;

namespace ImpliciX.Data.Tests.HotDb;

internal class Helpers
{
    internal static readonly string FolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "hotdb_test");
    internal const string DbName = "tests";

    internal const ushort HeaderSize = 256;
    public static void DbTest(
        Func<Data.HotDb.HotDb> arrange, 
        Action<Data.HotDb.HotDb> act, 
        Action<IReadOnlyRam> assertRam,
        Action<Disk> assertDisk
        )
    {
        DestroyDb(FolderPath);   
        Data.HotDb.HotDb hotDb = null;
        try
        {
            hotDb = arrange();
            act(hotDb);
            assertRam(hotDb.Ram);
            assertDisk(hotDb.Disk);
        }finally
        {
            if (hotDb != null)
            {
                hotDb.Dispose();
                DestroyDb(FolderPath);
            }
        }
    }
    
    public static void DbTest(
        Func<Data.HotDb.HotDb> arrange, 
        Func<Data.HotDb.HotDb,Data.HotDb.HotDb> act = null, 
        Action<IReadOnlyRam> assertRam=null,
        Action<Disk> assertDisk = null
    )
    {
        DestroyDb(FolderPath);   
        Data.HotDb.HotDb hotDb = null;
        act ??= o => o;
        assertDisk ??= _ => {};
        assertRam ??= _ => {};
        
        try
        {
            hotDb = arrange();
            hotDb = act(hotDb);
            assertRam(hotDb.Ram);
            assertDisk(hotDb.Disk);
        }finally
        {
            if (hotDb != null)
            {
                hotDb.Dispose();
                DestroyDb(FolderPath);
            }
        }
    }
    
    public static void DbTest<T>(
        Func<Data.HotDb.HotDb> arrange, 
        Func<Data.HotDb.HotDb,T> act, 
        Action<T> assertOutcome
    )
    {
        DestroyDb(FolderPath);
        Data.HotDb.HotDb hotDb = null;
        
        try
        {
            hotDb = arrange();
            var t = act(hotDb);
            assertOutcome(t);
        }finally
        {
            if (hotDb != null)
            {
                hotDb.Dispose();
                DestroyDb(FolderPath);
            }
        }
    }
    
    public static long GetFileSize(string filePath)
    {
        return new FileInfo(filePath).Length;
    }
    
    internal static void UpsertMany(Data.HotDb.HotDb hotDb, string structureName, (long pk, float value)[] dataPoints)
    {
        foreach (var dp in dataPoints)
        {
            var payloadBytes = GetBytes(dp.pk).Concat(GetBytes(dp.value)).ToArray();
            var structDef = hotDb.DefinedStructs.First(d => d.Name == structureName);
            hotDb.Upsert(structDef, dp.pk, payloadBytes);
        }
    }
    
    private static void DestroyDb(string folderPath)
    {
        if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
    }
    
    public static List<(BlockState, long, float)> ExtractBlocksData(byte[] blocksBytes, StructDef structDef)
    {
        return blocksBytes.Chunk(structDef.BlockDiskSize)
            .TakeWhile(b => b[0] != 0)
            .Select(ExtractBlockData)
            .ToList();
    }

    public static (BlockState, long, float) ExtractBlockData(byte[] bytes) => 
        ((BlockState)bytes[0], ToInt64(bytes[1..9]), ToSingle(bytes[9..13]));
    
    public static (long, float) DecodeBlockPayload(byte[] bytes) =>
        (ToInt64(bytes[..8]), ToSingle(bytes[8..]));
    
    public static byte[] EncodeBlockPayload(long pk, float value) =>
        GetBytes(pk).Concat(GetBytes(value)).ToArray();
}