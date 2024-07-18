using System;
using System.Collections.Generic;

namespace ImpliciX.Data.ColdDb;

public interface IColdCollectionReader<TDataPoint> :
  IDisposable
  where TDataPoint : IDataPoint
{
  IEnumerable<TDataPoint> DataPoints { get; }
  ColdMetaData MetaData { get; }
}
