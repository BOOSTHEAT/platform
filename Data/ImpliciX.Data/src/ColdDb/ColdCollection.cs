using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdDb;

public class ColdCollection<TDataPoint> : IColdCollection<TDataPoint> where TDataPoint : IDataPoint
{
  private readonly Header _header;

  public ColdCollection(
    string filePath,
    Urn dbUrn,
    IProtocol<TDataPoint> protocol
  )
  {
    if (string.IsNullOrEmpty(filePath))
      throw new ArgumentException("Value cannot be null or empty.", nameof(filePath));

    Io = new Io(filePath);
    Ram = new Ram();
    Protocol = protocol;
    if (Io.FsLength > 0)
      throw new InvalidOperationException($"File already exists and contains data: {filePath}");

    _header = new Header(Protocol.HeaderSize, Protocol.HeaderOffset);
    Io.WriteProtocolVersion(Protocol.Version);
    Io.AllocateHeader(Protocol.HeaderSize + Protocol.HeaderOffset);
    WriteMetaData(MetaDataItem.Urn(dbUrn));
  }

  public ColdCollection(
    string filePath,
    Func<byte, IProtocol<TDataPoint>> protocolFactory
  )
  {
    if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
      throw new ArgumentException($"File does not exists or is empty: {filePath}", nameof(filePath));

    Ram = new Ram();
    Io = new Io(filePath);
    try
    {
      Protocol = protocolFactory(Io.ReadByte());
      _header = new Header(Protocol.HeaderSize, Protocol.HeaderOffset);
      LoadMetaData();
    }
    catch
    {
      Io?.Dispose();
      throw;
    }
  }
  private bool IsDisposed { get; set; }

  private Io Io { get; }
  private Ram Ram { get; }
  public IProtocol<TDataPoint> Protocol { get; }

  private Dictionary<PropertyDescriptor, byte> KnownPropertiesIndex { get; } = new();
  public string FilePath => Io.FilePath;
  public IEnumerable<TDataPoint> DataPoints => ReadDataPoints2();

  public ColdMetaData MetaData => new(
    (Urn?)Ram.MetaDataItems.GetValueOrDefault((byte)MetaDataKind.Urn)?.Value,
    KnownPropertiesIndex.Keys.ToArray(),
    (TimeSpan?)Ram.MetaDataItems
      .GetValueOrDefault((byte)MetaDataKind.FirstDataPointTime)?.Value,
    (TimeSpan?)Ram.MetaDataItems.GetValueOrDefault((byte)MetaDataKind.LastDataPointTime)
      ?.Value,
    (long?)Ram.MetaDataItems.GetValueOrDefault((byte)MetaDataKind.DataPointsCount)?.Value ?? 0
  );

  public void WriteDataPoint(TDataPoint coldDataPoint)
  {
    if (IsDisposed)
      throw new ObjectDisposedException(nameof(ColdCollection<TDataPoint>));

    if (coldDataPoint.ValuesCount > Protocol.MaxNumberOfPropertiesPerDataPoint)
      throw new InvalidOperationException(
        $"Max number of properties per data point exceeded. Max :{Protocol.MaxNumberOfPropertiesPerDataPoint}; Actual: {coldDataPoint.ValuesCount}");

    if (MetaData.FirstDataPointTime is null) WriteMetaData(MetaDataItem.FirstDataItemPointTime(coldDataPoint.At));

    var missingProperties =
      coldDataPoint.PropertyDescriptors.Where(it => !KnownPropertiesIndex.ContainsKey(it)).ToArray();

    for (var i = 0; i < missingProperties.Length; i++)
      WriteMetaData(MetaDataItem.PropertyDescription(missingProperties[i]));

    Io.WriteBlock(Protocol.EncodeDataPoint(coldDataPoint, KnownPropertiesIndex,
      MetaData.LastDataPointTime ?? coldDataPoint.At));
    WriteMetaData(MetaDataItem.LastDataItemPointTime(coldDataPoint.At));
    WriteMetaData(MetaDataItem.DataPointsCount(MetaData.DataPointsCount + 1));
  }

  public ColdCollection<TDataPoint> StartNewFile()
  {
    var folder = Path.GetDirectoryName(Io.FilePath)!;
    var extension = Path.GetExtension(Io.FilePath);
    var newFilePath = Path.Combine(folder, $"{Guid.NewGuid()}{extension}");
    return new ColdCollection<TDataPoint>(newFilePath, MetaData.Urn!, Protocol);
  }


  public void Dispose()
  {
    if (IsDisposed) return;
    Io.Dispose();
    IsDisposed = true;
  }

  public bool Equals(IColdCollection<TDataPoint> other) => Io.FilePath == other?.FilePath;

  private void LoadMetaData()
  {
    Io.ReadMetaData(Protocol.HeaderOffset, Protocol.HeaderSize)
      .ToList()
      .ForEach(it =>
      {
        var (ptrIndex, valueOffset, bytes) = it;
        var md = Protocol.DecodeMetadata(bytes);
        var pointer = new MetaDataPointer(
          _header.GetPointerOffset(ptrIndex)!.Value,
          valueOffset,
          md.Kind,
          md.Value,
          bytes.Length);
        UpdateCache(md, pointer);
      });
  }

  private void WriteMetaData(MetaDataItem md)
  {
    if (!md.CanUpdate && Ram.MetaDataItems.ContainsKey(md.Kind))
      throw new InvalidOperationException($"{md.Kind} already written");

    var bytes = Protocol.EncodeMetadata(md);
    if (md.CanUpdate && Ram.MetaDataItems.TryGetValue(md.Kind, out var item))
    {
      var pointer = item with
      {
        Value = md.Value,
        Length = bytes.Length
      };

      Io.WriteBlock(pointer.ValueOffset, bytes);
      UpdateCache(md, pointer);
    }
    else
    {
      var pointerOffset = _header.GetPointerOffset(Ram.MetadataCount);
      if (pointerOffset == null) return;

      var pointer = new MetaDataPointer(pointerOffset.Value, (uint)Io.FsLength, md.Kind, md.Value, bytes.Length);
      Io.WritePointer(pointer.PointerOffset);
      Io.WriteBlock(bytes);
      UpdateCache(md, pointer);
    }
  }

  private IEnumerable<TDataPoint> ReadDataPoints2()
  {
    if (IsDisposed)
      throw new ObjectDisposedException(nameof(ColdCollection<TDataPoint>));

    var metaDataMap = Ram.MetaDataItems.Values.Concat(
        Ram.SetsMetaData.Values.SelectMany(it => it))
      .OrderBy(it => it.ValueOffset)
      .Select(it => (Address: it.ValueOffset, it.Length))
      .ToDictionary(it => (long)it.Address, it => Protocol.GetBlockLength(it.Length));

    var knownProperties = KnownPropertiesIndex.Keys.ToArray();
    var prevTime = MetaData.FirstDataPointTime ?? TimeSpan.Zero;
    foreach (var bytes in Io.ReadContent(Protocol.ContentOffset, metaDataMap))
    {
      var dp = Protocol.DecodeDataPoint(bytes, knownProperties, prevTime);
      prevTime = dp.At;
      yield return dp;
    }
  }


  private void UpdateCache(MetaDataItem md, MetaDataPointer pointer)
  {
    Ram.Update(md, pointer);

    if (!md.IsUnique && md.Kind == (byte)MetaDataKind.PropertyDescriptor)
      KnownPropertiesIndex[(PropertyDescriptor)md.Value] = (byte)KnownPropertiesIndex.Count;
  }

  protected bool Equals(ColdCollection<TDataPoint> other) => Equals(this, other);

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(null, obj)) return false;
    if (ReferenceEquals(this, obj)) return true;
    if (obj.GetType() != GetType()) return false;
    return Equals((ColdCollection<TDataPoint>)obj);
  }

  public override int GetHashCode() => FilePath != null ? FilePath.GetHashCode() : 0;

  public static bool operator ==(ColdCollection<TDataPoint> left, ColdCollection<TDataPoint> right) =>
    Equals(left, right);

  public static bool operator !=(ColdCollection<TDataPoint> left, ColdCollection<TDataPoint> right) =>
    !Equals(left, right);
}

public sealed class Header
{
  public Header(ushort headerSize, ushort headerOffset)
  {
    HeaderOffset = headerOffset;
    HeaderSize = headerSize;
  }

  public ushort HeaderSize { get; }
  public ushort HeaderOffset { get; }

  public uint? GetPointerOffset(uint index)
  {
    var pointerOffset = HeaderOffset + index * sizeof(uint);
    if (pointerOffset >= HeaderOffset + HeaderSize)
      return null;

    return pointerOffset;
  }
}
