#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ImpliciX.Data.HotDb.Model;

namespace ImpliciX.Data.HotDb;

public static class HotDbExport
{
    public static void ExportJson(string dbPath, string exportPath, Func<byte[], string,  object>? decodeFunc = null)
    {
        decodeFunc ??= (bytes,_) => Convert.ToHexString(bytes);
        var dbName = Path.GetFileNameWithoutExtension(Directory.EnumerateFiles(dbPath).First());
        var db = HotDb.Load(dbPath, dbName);
        var structDumps = new List<StructDump>();
        foreach (var structDef in db.DefinedStructs)
        {
            var structDump = new StructDump(
                name: structDef.Name,
                blockDiskSize:structDef.BlockDiskSize,
                blocksPerSegment: structDef.BlocksPerSegment,
                segments: new List<SegDump>());
            structDumps.Add(structDump);
            if (db.Ram.TryGetSegments(structDef, out var segments))
            {
                foreach (var seg in segments)
                {
                    var segDump = new SegDump(
                        usedSpace: seg.UsedSpace,
                        freeSpace: seg.FreeSpace,
                        diskSpace: seg.DiskSpace,
                        firstBlockOffset: seg.FirstBlockOffset,
                        lastBlockOffset: seg.LastBlockOffset,
                        state: seg.State,
                        blocks: new List<object?>());
                    structDump.Segments.Add(segDump);
                     
                    var segBytes = db.Disk.Blocks.Read(seg.FirstBlockOffset, seg.DiskSpace);
                    segBytes.Chunk(structDef.BlockDiskSize)
                        .Select(BlockDump.FromBytes)
                        .Select(bd => bd != null? new{state=bd.State, data = decodeFunc(bd.Bytes, structDef.Name)} : null)
                        .ToList()
                        .ForEach(segDump.Blocks.Add);
                }
            }
        }
        var dumpsJson = JsonSerializer.Serialize(structDumps.OrderByDescending(s=>s.SegmentsCount));
        File.WriteAllText(exportPath, dumpsJson);

        
        
    }
    
    internal class StructDump
    {
        public StructDump(string name, ushort blockDiskSize, ushort blocksPerSegment, List<SegDump> segments)
        {
            Name = name;
            BlockDiskSize = blockDiskSize;
            BlocksPerSegment = blocksPerSegment;
            Segments = segments;
        }

        public string Name { get; set; }
        public ushort BlockDiskSize { get; }
        public ushort BlocksPerSegment { get; }
        public int SegmentsCount => Segments.Count;
        public List<SegDump> Segments { get; set; }
    }
    internal class SegDump
    {
        public SegDump(ushort usedSpace,
            ushort freeSpace,
            ushort diskSpace,
            uint firstBlockOffset,
            uint lastBlockOffset,
            SegmentState state,
            List<object?>? blocks = null)
        {
            UsedSpace = usedSpace;
            FreeSpace = freeSpace;
            DiskSpace = diskSpace;
            FirstBlockOffset = firstBlockOffset;
            LastBlockOffset = lastBlockOffset;
            State = state.ToString();
            Blocks = blocks ?? new List<object?>();
        }

        public ushort UsedSpace { get; init; }
        public ushort FreeSpace { get; init; }
        public ushort DiskSpace { get; init; }
        public uint FirstBlockOffset { get; init; }
        public uint LastBlockOffset { get; init; }
        public string State { get; init; }
        
        public List<object?> Blocks { get; set; }
        
        
    }
    internal record BlockDump(string State,byte[] Bytes)
    {
        public static BlockDump? FromBytes(byte[] bytes)
        {
            if(bytes[0]==0) return null;
            return new BlockDump(((BlockState)bytes[0]).ToString(),bytes[1..]);
        }
    }

}