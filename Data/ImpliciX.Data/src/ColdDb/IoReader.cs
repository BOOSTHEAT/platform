using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.Data.Tools;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.ColdDb;

public class IoReader
{
  public IoReader(IStoreFile file)
  {
    File = file;
    RFs = file.OpenReadAsync().Result;
    Reader = new BinaryReader(RFs);
  }
  private BinaryReader Reader { get; }
  private Stream RFs { get; }
  private IStoreFile File { get; }
  private bool IsDisposed { get; set; }
  public void Dispose()
  {
    if (IsDisposed) return;
    RFs.Dispose();
    Reader.Dispose();
    IsDisposed = true;
  }
  public byte ReadByte() => Reader.ReadByte();

  private byte[] ReadBlock(uint offset)
  {
    RFs.Seek(offset, SeekOrigin.Begin);
    var lengthBytes = Reader.ReadBytes(2);
    if (lengthBytes[0] == 0 && lengthBytes[1] == 0)
      return Array.Empty<byte>();
    var length = BitConverter.ToUInt16(lengthBytes);
    var bytes = Reader.ReadBytes(length);
    return bytes;
  }

  public IEnumerable<(uint, uint, byte[])> ReadMetaData(int headerOffset, int headerSize)
  {
    RFs.Seek(headerOffset, SeekOrigin.Begin);
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

  public IEnumerable<byte[]> ReadContent(int contentOffset, Dictionary<long, int> blocksToSkip)
  {
    RFs.Seek(contentOffset, SeekOrigin.Begin);
    while (!RFs.IsAtTheEnd())
      if (blocksToSkip.TryGetValue(RFs.Position, out var blockLength))
      {
        RFs.Seek(blockLength, SeekOrigin.Current);
      }
      else
      {
        var bytes = ReadBlock((uint)RFs.Position);
        if (bytes.Length == 0)
        {
          Log.Error("[ColdDb] file {@file} is corrupted after position {@position}.", File.LocalPath,
            RFs.Position);
          yield break;
        }
        yield return bytes;
      }
  }
}
