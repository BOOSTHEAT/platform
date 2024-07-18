using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotDb.Hdw;
using static System.BitConverter;
using static ImpliciX.Data.HotDb.Model.BlockState;

namespace ImpliciX.Data.HotDb.Model;

internal class Scribe
{
    public WriteContext DefineDataStructure(StructDef def, WriteContext ctx, Disk disk)
    {
        var payloadOffset = disk.Structure.EndOfFileOffset;
        disk.Structure.Seek(payloadOffset, SeekOrigin.Begin);
        var payLoad = def.ToBytes();    
        disk.Structure.Write(GetBytes((ushort)payLoad.Length).Concat(payLoad).ToArray());
        ctx.StructDef = def;
        return ctx;
    }

    public IEnumerable<(Seg segment, Block block)> ReadForwardBlocks(StructDef structDef, Seg seg, Disk disk)
    {
        if (seg == null) return Enumerable.Empty<(Seg segment, Block block)>();
        return
            Enumerable.Range(0, structDef.BlocksPerSegment)
                .Select(i =>
                {
                    var offset = (uint)(seg.FirstBlockOffset + i * structDef.BlockDiskSize);
                    var blockBytes = disk.Blocks.Read(offset, 9); // 1 byte for state, 8 bytes for pk
                    var state = (BlockState)blockBytes[0];
                    return (state, offset, blockBytes);
                })
                .Where(it=>it.state == NotDeleted)
                .TakeWhile(it => it.offset <= seg.LastBlockOffset)
                .Select(it => (seg, new Block(it.state, it.offset, ToInt64(it.blockBytes[1..]))));
    }
    
    public IEnumerable<(Seg segment, Block block)> ReadBackwardBlocks(StructDef structDef, Seg seg, Disk disk)
    {
        if (seg == null) return Enumerable.Empty<(Seg segment, Block block)>();
        return
            Enumerable.Range(0, structDef.BlocksPerSegment)
                .Select(i =>
                {
                    var offset = (uint)(seg.LastBlockOffset - i * structDef.BlockDiskSize);
                    var blockBytes = disk.Blocks.Read(offset, 9); // 1 byte for state, 8 bytes for pk
                    var state = (BlockState)blockBytes[0];
                    return (state,offset, blockBytes);
                })
                .Where(it=>it.state == NotDeleted)
                .TakeWhile(it => it.offset >= seg.FirstBlockOffset)
                .Select(it => (seg, new Block((BlockState)it.blockBytes[0], it.offset, ToInt64(it.blockBytes[1..]))));
    }
    
    public IEnumerable<(StructDef structDef, Page page)> ReadPagesOfSeg(StructDef structDef, Seg seg, Disk disk)
    {
        if (seg == null) return Enumerable.Empty<(StructDef structDef, Page page)>();
        return
            Enumerable.Range(0, structDef.BlocksPerSegment)
                .Select(i =>
                {
                    var offset = (uint)(seg.FirstBlockOffset + i * structDef.BlockDiskSize);
                    var blockBytes = disk.Blocks.Read(offset, structDef.BlockDiskSize); 
                    return (offset, blockBytes);
                }).TakeWhile(it => it.blockBytes[0] != 0)
                .Where(it=>it.blockBytes[0] == (byte)NotDeleted)
                .Select(it => (structDef, new Page(ToInt64(it.blockBytes[1..9]), it.blockBytes[1..])));
    }
    
    public (Block, bool) GetBlockOrNew(long pk, Seg seg, Block newBlock, IReadOnlyRam ram, Disk disk)
    {
        var lastPk = ram.LastPkOf(seg.StructDefId) ?? long.MinValue;
        
        var oldBlock = pk > lastPk ? null : ReadForwardBlocks(ram.GetStructureById(seg.StructDefId), seg, disk)
            .Select(it => it.block)
            .FirstOrDefault(it => it.Pk == pk);
        
        return oldBlock?.State switch
        {
            null => (newBlock, true),
            NotDeleted => (oldBlock, false),
            Deleted => (oldBlock with {State = NotDeleted}, true),
            _ => throw new Exception("invalid block state")
        };
    }
    
    public (BlockState state, byte[] payload) ReadBlockPayload(StructDef structDef, Disk disk, Block block)
    {
        var payLoadBytes = disk.Blocks.Read(block.OwnOffset, structDef.BlockDiskSize);
        return (state: (BlockState) payLoadBytes[0], payLoadBytes[1..]);
    }
    
    public WriteContext FreeBlock(Disk disk, WriteContext ctx)
    { 
        var block = ctx.Block!;
        disk.Blocks.Seek(block.StateByteOffset, SeekOrigin.Begin);
        disk.Blocks.Write((byte) Deleted);
        return ctx.SetResults(1);
    }
    
    public BulkDeleteContext FreeBlocks(BulkDeleteContext ctx, Disk disk)
    {
        foreach (var block in ctx.Blocks!)
        {
            disk.Blocks.Seek(block.StateByteOffset, SeekOrigin.Begin);
            disk.Blocks.Write((byte) Deleted);
        }
        return ctx.SetResults(ctx.Blocks!.Length);
    }
    
    public WriteContext AllocateSeg(Disk disk, WriteContext ctx)
    {
        disk.Blocks.Allocate(ctx.Seg!.DiskSpace);
        return ctx;
    }

    public WriteContext WriteBlock(byte[] bytes, Disk disk, WriteContext ctx)
    {
        var block = ctx.Block!;
        disk.Blocks.Seek(block.OwnOffset, SeekOrigin.Begin);
        disk.Blocks.Write(block.ToBytes(bytes));
        return ctx;
    }

    public WriteContext WriteSeg(Disk disk, WriteContext ctx)
    {
        WriteSeg(disk, ctx.Seg!);
        return ctx;
    }
    
    public BulkDeleteContext WriteSegs(Disk disk, BulkDeleteContext ctx)
    {
        foreach (var seg in ctx.Segs!) WriteSeg(disk, seg);
        return ctx;
    }


    private static void WriteSeg(Disk disk, Seg seg)
    {
        var bytes = seg.ToBytes();
        disk.Segments.Seek(seg.OwnOffset, SeekOrigin.Begin);
        disk.Segments.Write(GetBytes((ushort) bytes.Length).Concat(bytes).ToArray());
    }

    
    internal static void ReadDb(Disk disk, IRam ram)
    {
        var allStructuresBytes = disk.Structure.ReadAllBytes();
        using var structReader = new BinaryReader(new MemoryStream(allStructuresBytes));
        while (!structReader.BaseStream.IsAtTheEnd())
        {
            var structSize = structReader.ReadUInt16();
            var structBytes = structReader.ReadBytes(structSize);
            var def = StructDef.FromBytes(structBytes);
            ram.AddOrUpdate(def);
        }
        
        var allSegmentsBytes = disk.Segments.ReadAllBytes();
        using var segReader = new BinaryReader(new MemoryStream(allSegmentsBytes));
        while (!segReader.BaseStream.IsAtTheEnd())
        {
            var segSize = segReader.ReadUInt16();
            var segBytes = segReader.ReadBytes(segSize);
            var seg = Seg.FromBytes(segBytes);
            ram.AddOrUpdate(seg);
        }
    }
}