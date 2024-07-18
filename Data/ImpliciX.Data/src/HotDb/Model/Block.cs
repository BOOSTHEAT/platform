#nullable enable
using System.IO;

namespace ImpliciX.Data.HotDb.Model;

public enum BlockState: byte
{
    Deleted = 127,
    NotDeleted = 128
}

internal record Page(long Pk, byte[] Bytes);

internal record Block(BlockState State, uint OwnOffset, long Pk)
{
    public uint StateByteOffset { get; } = OwnOffset;

    public byte[] ToBytes(byte[] bytes)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((byte)State);
        bw.Write(bytes);
        bw.Flush();
        return ms.ToArray();
    }
    internal static ushort ComputeDiskSize(int payloadSize) => (ushort) (sizeof(byte)+payloadSize);

    public bool InRange(long pkMin, long pkMax)
    {
        return Pk >= pkMin && Pk <= pkMax;
    }
}