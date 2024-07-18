using System;

namespace ImpliciX.Data.ColdDb;

public interface IColdCollection<TDataPoint> :
  IColdCollectionReader<TDataPoint>,
  IEquatable<IColdCollection<TDataPoint>>
  where TDataPoint : IDataPoint
{
  string FilePath { get; }
  void WriteDataPoint(TDataPoint coldDataPoint);
  ColdCollection<TDataPoint> StartNewFile();
}
