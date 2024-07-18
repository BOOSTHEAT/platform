using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.Tools;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdDb;

public class ColdCollectionReader<TDataPoint> :
  IColdCollectionReader<TDataPoint>
  where TDataPoint : IDataPoint
{
  private readonly BinaryReader _binaryReader;
  private readonly Header _header;
  private readonly Dictionary<byte, MetaDataPointer> _metaDataItems = new();
  private readonly ConcurrentDictionary<byte, List<MetaDataPointer>> _setsMetaData = new();

  public ColdCollectionReader(
    IStoreFile file,
    Func<byte, IProtocol<TDataPoint>> protocolFactory
  )
  {
    _binaryReader = new BinaryReader(file.OpenReadAsync().Result);


    Io = new IoReader(file);
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

  private IoReader Io { get; }

  public IProtocol<TDataPoint> Protocol { get; }

  private Dictionary<PropertyDescriptor, byte> KnownPropertiesIndex { get; } = new();
  private uint MetadataCount => (uint)(_metaDataItems.Count + _setsMetaData.Sum(it => it.Value.Count));
  public IEnumerable<TDataPoint> DataPoints => ReadDataPoints2();

  public ColdMetaData MetaData => new(
    (Urn?)_metaDataItems.GetValueOrDefault((byte)MetaDataKind.Urn)?.Value,
    KnownPropertiesIndex.Keys.ToArray(),
    (TimeSpan?)_metaDataItems
      .GetValueOrDefault((byte)MetaDataKind.FirstDataPointTime)?.Value,
    (TimeSpan?)_metaDataItems.GetValueOrDefault((byte)MetaDataKind.LastDataPointTime)
      ?.Value,
    (long?)_metaDataItems.GetValueOrDefault((byte)MetaDataKind.DataPointsCount)?.Value ?? 0
  );

  public void Dispose()
  {
    if (IsDisposed) return;
    Io.Dispose();
    IsDisposed = true;
  }

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

  private IEnumerable<TDataPoint> ReadDataPoints2()
  {
    if (IsDisposed)
      throw new ObjectDisposedException(nameof(ColdCollection<TDataPoint>));

    var metaDataMap = _metaDataItems.Values.Concat(
        _setsMetaData.Values.SelectMany(it => it))
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
    Update(md, pointer);

    if (!md.IsUnique && md.Kind == (byte)MetaDataKind.PropertyDescriptor)
      KnownPropertiesIndex[(PropertyDescriptor)md.Value] = (byte)KnownPropertiesIndex.Count;
  }
  private void Update(MetaDataItem md, MetaDataPointer pointer)
  {
    if (md.IsUnique)
    {
      _metaDataItems[md.Kind] = pointer;
    }
    else
    {
      _setsMetaData.AddOrUpdate(md.Kind, new List<MetaDataPointer> {pointer}, (key, list) =>
      {
        list.Add(pointer);
        return list;
      });
    }
  }
}
