#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static ImpliciX.Data.HotDb.Model.SegmentState;

namespace ImpliciX.Data.HotDb.Model;

public enum SegmentState:byte
{
    Reusable = 111,
    InUse = 112,
    NonUsable=113
}

public record Seg(
    uint OwnOffset, 
    Guid StructDefId,
    ushort UsedSpace,
    ushort FreeSpace,
    ushort DiskSpace,
    uint FirstBlockOffset,
    uint LastBlockOffset,
    long FirstPk, 
    long LastPk,
    SegmentState State)
{
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(OwnOffset);
        bw.Write(StructDefId.ToString());
        bw.Write(UsedSpace);
        bw.Write(FreeSpace);
        bw.Write(DiskSpace);
        bw.Write(FirstBlockOffset);
        bw.Write(LastBlockOffset);
        bw.Write(FirstPk);
        bw.Write(LastPk);
        bw.Write((byte)State);
        bw.Flush();
        return ms.ToArray();
    }
    
    public static Seg FromBytes(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        return new Seg(
            OwnOffset: br.ReadUInt32(),
            StructDefId: Guid.Parse(br.ReadString()),
            UsedSpace: br.ReadUInt16(),
            FreeSpace: br.ReadUInt16(),
            DiskSpace: br.ReadUInt16(),
            FirstBlockOffset: br.ReadUInt32(),
            LastBlockOffset: br.ReadUInt32(),
            FirstPk: br.ReadInt64(),
            LastPk: br.ReadInt64(),
            State: (SegmentState)br.ReadByte()
        );
    }
    
    public static int SizeOnDisk = sizeof(ushort) + //payloadSize
                                    sizeof(uint) + //offset
                                   37 + // Guid
                                   3 * sizeof(ushort) + 
                                   2 * sizeof(uint) + 
                                   2 * sizeof(long) + 
                                   1 * sizeof(byte);
    
    internal Seg AddBlock(Block block, int bytesLength)
    {
        var usedSpace = (ushort)(UsedSpace + Block.ComputeDiskSize(bytesLength));
        var freeSpace = (ushort)(FreeSpace - Block.ComputeDiskSize(bytesLength));
        return this with 
        {
            UsedSpace = usedSpace, 
            FreeSpace = freeSpace,
            FirstPk = State== Reusable ? block.Pk : FirstPk,
            LastPk = block.Pk,
            State = usedSpace == DiskSpace ? NonUsable : InUse,
            FirstBlockOffset = State == Reusable ? block.OwnOffset : FirstBlockOffset,
            LastBlockOffset = block.OwnOffset
        };
    }

    public bool ContainsPk(long pk)
    {
        return pk >= FirstPk && pk <= LastPk;
    }


    internal Seg FreeBlocks(StructDef structDef, params Block[] blocks)
    {
        if (State == Reusable) return this;
        var freeSpace = (ushort)(FreeSpace + blocks.Sum(_=>structDef.BlockDiskSize));
        var newState = State switch
        {
            Reusable => Reusable,
            InUse => freeSpace == DiskSpace ? Reusable : InUse,
            NonUsable => freeSpace == DiskSpace ? Reusable : NonUsable,
            _ => throw new ArgumentOutOfRangeException()
        };
        return this with 
        {
            FreeSpace = freeSpace,
            UsedSpace = newState == Reusable ? (ushort)0 : UsedSpace,
            FirstPk = newState == Reusable ? (ushort)0: FirstPk,
            LastPk  = newState == Reusable ? (ushort)0: LastPk,
            LastBlockOffset = newState == Reusable ? FirstBlockOffset : LastBlockOffset,
            State = newState
        };
    }

    public bool Overlaps(long pkMin, long pkMax) => 
        Math.Max(pkMin, FirstPk) <= Math.Min(pkMax, LastPk);

    public virtual bool Equals(Seg? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return OwnOffset == other.OwnOffset;
    }

    public override int GetHashCode()
    {
        return (int) OwnOffset;
    }
}

public class ByPkComparer: IComparer<Seg>
{
    public int Compare(Seg? x, Seg? y)
    {
        if (x is null || y is null) return 0;
        return x.FirstPk.CompareTo(y.FirstPk);
    }
}

public class ByOwnOffsetComparer: IComparer<Seg>
{
    public int Compare(Seg? x, Seg? y)
    {
        if (x is null || y is null) return 0;
        return x.OwnOffset.CompareTo(y.OwnOffset);
    }
}