using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.Data.ColdDb;

public sealed class Ram
{
    private readonly Dictionary<byte, MetaDataPointer> _metaDataItems  = new ();
    private readonly ConcurrentDictionary<byte, List<MetaDataPointer>> _setsMetaData = new ();

    public IReadOnlyDictionary<byte, MetaDataPointer> MetaDataItems => _metaDataItems;
    public IReadOnlyDictionary<byte, List<MetaDataPointer>> SetsMetaData => _setsMetaData;
    public uint MetadataCount => (uint) (MetaDataItems.Count + SetsMetaData.Sum(it => it.Value.Count));

    public void Update(MetaDataItem md, MetaDataPointer pointer)
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