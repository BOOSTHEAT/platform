using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdMetrics;

public class FileTimeIndex
{
  private readonly HashSet<IColdCollection<MetricsDataPoint>> _data = new ();

  public FileTimeIndex Add(
    IColdCollection<MetricsDataPoint> collection
  )
  {
    _data.Add(collection);
    return this;
  }


  public void Replace(
    IColdCollection<MetricsDataPoint> oldCollection,
    IColdCollection<MetricsDataPoint> newCollection
  )
  {
    _data.Remove(oldCollection);
    _data.Add(newCollection);
  }

  public IColdCollection<MetricsDataPoint>[] CollectionsInRange(
    long firstDataPoint,
    long lastDataPoint
  )
  {
    bool Overlaps(
      long fst1,
      long lst1,
      long fst2,
      long lst2
    )
    {
      return fst1 <= lst2 && lst1 >= fst2;
    }

    long[] GetRange(
      IColdCollection<MetricsDataPoint> collection
    )
    {
      return new[]
      {
        collection.MetaData.FirstDataPointTime.GetValueOrDefault(TimeSpan.MinValue).Ticks,
        collection.MetaData.LastDataPointTime.GetValueOrDefault(TimeSpan.MaxValue).Ticks
      };
    }

    return (
        from collection in _data
        let range = GetRange(collection)
        where Overlaps(
          range[0],
          range[1],
          firstDataPoint,
          lastDataPoint
        )
        select collection)
      .ToArray();
  }

  internal void purge(
    IColdCollection<MetricsDataPoint> collection
  )
  {
    _data.Remove(collection);
  }
}

public class InMemoryTimeIndex
{
  private readonly ConcurrentDictionary<Urn, FileTimeIndex> _data = new ();

  public InMemoryTimeIndex Add(
    IColdCollection<MetricsDataPoint> collection
  )
  {
    _data.AddOrUpdate(
      collection.MetaData.Urn!,
      new FileTimeIndex().Add(collection),
      (
        _,
        fti
      ) => fti.Add(collection)
    );
    return this;
  }

  public void Replace(
    IColdCollection<MetricsDataPoint> oldCollection,
    IColdCollection<MetricsDataPoint> finishedCollection
  )
  {
    _data[oldCollection.MetaData.Urn!].Replace(
      oldCollection,
      finishedCollection
    );
  }


  public bool ContainsMetric(
    Urn urn
  )
  {
    return _data.ContainsKey(urn);
  }

  public IColdCollection<MetricsDataPoint>[] CollectionsInRange(
    Urn urn,
    TimeSpan firstDataPoint,
    TimeSpan lastDataPoint
  )
  {
    if (!_data.TryGetValue(
          urn,
          out var fileTimeIndex
        ))
      return Array.Empty<IColdCollection<MetricsDataPoint>>();
    return fileTimeIndex.CollectionsInRange(
      firstDataPoint.Ticks,
      lastDataPoint.Ticks
    );
  }

  internal void purge(
    IColdCollection<MetricsDataPoint> collection
  )
  {
    _data[collection.MetaData.Urn!].purge(collection);
  }
}
