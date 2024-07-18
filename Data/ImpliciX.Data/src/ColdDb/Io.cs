using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.ColdDb;

public sealed class Io : IDisposable
{
  public Io(string filePath)
  {
    FilePath = filePath;
    RwFs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
    Writer = new BinaryWriter(RwFs);
    Reader = new BinaryReader(RwFs);
  }
  private bool IsDisposed { get; set; }
  private BinaryReader Reader { get; }
  private FileStream RwFs { get; }
  private BinaryWriter Writer { get; }
  public string FilePath
  {
    get;
  }
  public long FsLength => RwFs.Length;

  public void Dispose()
  {
    if (IsDisposed) return;
    Writer.Flush();
    Log.Information("[ColdDb] ForceFlush {@FilePath}", FilePath);
    RwFs.Flush(true);
    RwFs.Dispose();
    Reader.Dispose();
    Writer?.Dispose();
    Log.Information("[ColdDb] Disposed {@FilePath}", FilePath);
    IsDisposed = true;
  }

  public byte ReadByte() => Reader.ReadByte();
  public static byte ReadProtocolVersion(string filePath)
  {
    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    return (byte)fs.ReadByte();
  }

  public void AllocateHeader(long size)
  {
    RwFs.SetLength(size);
    RwFs.Seek(0, SeekOrigin.End);
  }

  public void WriteProtocolVersion(byte pv)
  {
    RwFs.Seek(0, SeekOrigin.Begin);
    RwFs.WriteByte(pv);
  }

  public IEnumerable<(uint, uint, byte[])> ReadMetaData(int headerOffset, int headerSize)
  {
    RwFs.Seek(headerOffset, SeekOrigin.Begin);
    var buffer = Reader.ReadBytes(headerSize);
    using var headerStream = new MemoryStream(buffer);
    using var headerReader = new BinaryReader(headerStream);
    var valueOffset = headerReader.ReadUInt32();
    var pointerIndex = 0u;

    while (!headerStream.IsAtTheEnd() && valueOffset != 0)
    {
      var bytes = ReadBlock(valueOffset);
      yield return (pointerIndex, valueOffset, bytes);
      valueOffset = headerReader.ReadUInt32();
      pointerIndex += 1;
    }
  }

  private byte[] ReadBlock(uint offset)
  {
    RwFs.Seek(offset, SeekOrigin.Begin);
    var lengthBytes = Reader.ReadBytes(2);
    if (lengthBytes[0] == 0 && lengthBytes[1] == 0)
      return Array.Empty<byte>();
    var length = BitConverter.ToUInt16(lengthBytes);
    var bytes = Reader.ReadBytes(length);
    return bytes;
  }

  public static byte[] ReadBlock(uint offset, Stream stream)
  {
    var br = new BinaryReader(stream);
    stream.Seek(offset, SeekOrigin.Begin);
    var lengthBytes = br.ReadBytes(2);
    if (lengthBytes[0] == 0 && lengthBytes[1] == 0)
      return Array.Empty<byte>();
    var length = BitConverter.ToUInt16(lengthBytes);
    var bytes = br.ReadBytes(length);
    return bytes;
  }

  public void WriteBlock(byte[] bytes)
  {
    RwFs.Seek(0, SeekOrigin.End);
    Writer.Write((ushort)bytes.Length);
    Writer.Write(bytes);
    Writer.Flush();
  }

  public void WriteBlock(uint address, byte[] bytes)
  {
    RwFs.Seek(address, SeekOrigin.Begin);
    Writer.Write((ushort)bytes.Length);
    Writer.Write(bytes);
    Writer.Flush();
  }

  public void WritePointer(long pointerOffset)
  {
    RwFs.Seek(pointerOffset, SeekOrigin.Begin);
    Writer.Write(RwFs.Length);
    Writer.Flush();
  }

  public IEnumerable<byte[]> ReadContent(int contentOffset, Dictionary<long, int> blocksToSkip)
  {
    RwFs.Seek(contentOffset, SeekOrigin.Begin);
    while (!RwFs.IsAtTheEnd())
      if (blocksToSkip.TryGetValue(RwFs.Position, out var blockLength))
      {
        RwFs.Seek(blockLength, SeekOrigin.Current);
      }
      else
      {
        var bytes = ReadBlock((uint)RwFs.Position);
        if (bytes.Length == 0)
        {
          Log.Error("[ColdDb] file {@filePath} is corrupted after position {@position}.", FilePath,
            RwFs.Position);
          yield break;
        }
        yield return bytes;
      }
  }
}
