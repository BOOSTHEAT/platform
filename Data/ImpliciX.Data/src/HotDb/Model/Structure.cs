#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImpliciX.Data.HotDb.Model;

public record FieldDef(string Name, string ModelTypeName, byte StorageType, int FixedSizeInBytes);

public record StructDef
{
    public StructDef(Guid Id, string Name, ushort BlocksPerSegment, FieldDef[] Fields)
    {
        this.Id = Id;
        this.Name = Name;
        this.Fields = Fields;
        BlockPayloadSize = (ushort)Fields.Sum(f => f.FixedSizeInBytes);
        this.BlocksPerSegment = BlocksPerSegment;
        BlockDiskSize = Block.ComputeDiskSize(BlockPayloadSize);
        DiskCapacityPerSegment = (ushort)(BlocksPerSegment * BlockDiskSize);
    }

    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(Id.ToString());
        bw.Write(Name);
        bw.Write(BlockPayloadSize);
        bw.Write(BlocksPerSegment);
        foreach (var field in Fields)
        {
            bw.Write(field.Name);
            bw.Write(field.ModelTypeName);
            bw.Write(field.StorageType);
            bw.Write(field.FixedSizeInBytes);
        }
        bw.Flush();
        return ms.ToArray();
    }
    
    public static StructDef FromBytes(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        var id = Guid.Parse(br.ReadString());
        var name = br.ReadString();
        var blockPayloadSize = br.ReadUInt16();
        var blocksPerSegment = br.ReadUInt16();
        var fields = new List<FieldDef>();
        while (ms.Position < ms.Length)
        {
            var fieldName = br.ReadString();
            var modelFieldType = br.ReadString();
            var storageType = br.ReadByte();
            var fieldSize = br.ReadInt32();
            fields.Add(new FieldDef(fieldName, modelFieldType, storageType, fieldSize));
        }
        return new StructDef(id, name, blocksPerSegment, fields.ToArray());
    }

    public ushort BlockDiskSize { get; init; }

    public ushort DiskCapacityPerSegment { get; }
    public Guid Id { get; init; }
    public string Name { get; init; }
    public ushort BlockPayloadSize { get; init; }
    public ushort BlocksPerSegment { get; init; }
    public FieldDef[] Fields { get; init; }
    public static IEqualityComparer<StructDef> StructuralCmp { get; } = new StructuralComparer();

    public virtual bool Equals(StructDef? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return
            Id == other.Id && 
            DiskCapacityPerSegment == other.DiskCapacityPerSegment 
               && Fields.SequenceEqual(other.Fields) 
               && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Id,
            DiskCapacityPerSegment, 
            Fields.Aggregate(0, HashCode.Combine),
            Name);
    }
    
    public class StructuralComparer : IEqualityComparer<StructDef>
    {
        public bool Equals(StructDef? x, StructDef? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.DiskCapacityPerSegment == y.DiskCapacityPerSegment 
                   && x.Fields.SequenceEqual(y.Fields) 
                   && x.Name == y.Name;;
        }

        public int GetHashCode(StructDef obj)
        {
            return HashCode.Combine(
                obj.DiskCapacityPerSegment, 
                obj.Fields.Aggregate(0, HashCode.Combine),
                obj.Name);
        }
    }
    
}