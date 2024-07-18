using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.Tools;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdDb;

public class ColdReader<T> where T : IDataPoint
{
  private readonly ConcurrentDictionary<Urn, SortedList<TimeSpan, Func<IColdCollectionReader<T>>>>
    _collectionFactories = new();

  //TODO: suppress ?
  public ColdReader(string folderPath, string fileExt, Func<string, IColdCollectionReader<T>> collectionFactory)
  {
    foreach (var file in Directory.GetFiles(folderPath, $"*{fileExt}"))
    {
      using var collection = collectionFactory(file);
      var fdpt = collection.MetaData.FirstDataPointTime.GetValueOrDefault();
      _collectionFactories.AddOrUpdate(collection.MetaData.Urn!,
        _ => new SortedList<TimeSpan, Func<IColdCollectionReader<T>>> {{fdpt, () => collectionFactory(file)}},
        (_, list) =>
        {
          list.Add(fdpt, () => collectionFactory(file));
          return list;
        });
    }
  }

  public ColdReader(IStoreFolder folderPath, string fileExt,
    Func<IStoreFile, IColdCollectionReader<T>> collectionFactory)
  {
    foreach (var file in IStoreFolder.GetFiles(folderPath, fileExt).Result)
    {
      using var collection = collectionFactory(file);
      var fdpt = collection.MetaData.FirstDataPointTime.GetValueOrDefault();
      _collectionFactories.AddOrUpdate(collection.MetaData.Urn!,
        _ => new SortedList<TimeSpan, Func<IColdCollectionReader<T>>> {{fdpt, () => collectionFactory(file)}},
        (_, list) =>
        {
          list.Add(fdpt, () => collectionFactory(file));
          return list;
        });
    }
  }

  public string[] Urns => _collectionFactories.Keys.Select(it => it.Value).OrderBy(it => it).ToArray();

  public string[] GetProperties(Urn urn)
  {
    return _collectionFactories.GetValueOrDefault(urn, new SortedList<TimeSpan, Func<IColdCollectionReader<T>>>())
      .SelectMany(it =>
      {
        using var c = it.Value();
        return c.MetaData.PropertyDescriptors!.Select(p => p.Urn.Value).ToArray();
      }).Distinct().ToArray();
  }

  public PropertyDescriptor[] GetPropertiesDescriptors(Urn urn)
  {
    return _collectionFactories.GetValueOrDefault(urn, new SortedList<TimeSpan, Func<IColdCollectionReader<T>>>())
      .SelectMany(it =>
      {
        using var c = it.Value();
        return c.MetaData.PropertyDescriptors!.ToArray();
      }).DistinctBy(p => p.Urn.Value).ToArray();
  }


  public IEnumerable<T> ReadDataPoints(Urn urn)
  {
    return _collectionFactories.GetValueOrDefault(urn, new SortedList<TimeSpan, Func<IColdCollectionReader<T>>>())
      .SelectMany(it =>
      {
        using var c = it.Value();
        return c.DataPoints.ToArray();
      });
  }
}
