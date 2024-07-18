using System;
using System.Collections.Generic;

namespace ImpliciX.Data.ColdDb;

public interface IDataPoint
{
    TimeSpan At { get; init; }
    PropertyDescriptor[] PropertyDescriptors { get; init; }
    int ValuesCount { get; init; }
}

public interface IProtocol<TDataPoint> where TDataPoint:IDataPoint
{
    byte Version { get; }
    ushort HeaderSize { get; }
    ushort HeaderOffset { get; }
    int ContentOffset { get; }
    ushort MaxNumberOfPropertiesPerDataPoint { get; }
    int GetBlockLength(int contentLength);
    byte[] EncodeMetadata(MetaDataItem item);
    MetaDataItem DecodeMetadata(byte[] bytes);
    byte[] EncodeDataPoint(TDataPoint dataPoint, Dictionary<PropertyDescriptor, byte> propertyDescriptor, TimeSpan prevTime);
    TDataPoint DecodeDataPoint(byte[] bytes, PropertyDescriptor[] propertyDescriptors, TimeSpan prevTime);
}