using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Language.Model;
using static System.BitConverter;
using static System.Text.Encoding;
using static ImpliciX.Data.ColdDb.MetaDataItem;
using static ImpliciX.Language.Model.Urn;

namespace ImpliciX.Data.Records.ColdRecords;

internal static class ProtocolFactory
{
    public static IProtocol<RecordsDataPoint> Create(byte version) => version switch
    {
        1 => new RecordsProtocolVersion1(),
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Protocol version not supported")
    };
}

internal sealed class RecordsProtocolVersion1 : IProtocol<RecordsDataPoint>
{
    public byte Version { get; } = 1;
    private byte[] FixedMetaDataItems { get; }
    public ushort MaxNumberOfPropertiesPerDataPoint { get; }
    private ushort NumberOfPointers { get; } = 128;
    public ushort HeaderSize { get; }
    public ushort HeaderOffset { get; }
    public int ContentOffset => HeaderOffset + HeaderSize;


    public RecordsProtocolVersion1()
    {
        FixedMetaDataItems = new[]
        {
            (byte) MetaDataKind.Urn, (byte) MetaDataKind.DataPointsCount, (byte) MetaDataKind.FirstDataPointTime,
            (byte) MetaDataKind.LastDataPointTime
        };
        HeaderOffset = 1 * sizeof(byte);
        HeaderSize = (ushort) (NumberOfPointers * sizeof(uint));
        MaxNumberOfPropertiesPerDataPoint = (ushort) (NumberOfPointers - FixedMetaDataItems.Length);
    }

    public int GetBlockLength(int contentLength) => sizeof(ushort) + contentLength;

    public byte[] EncodeMetadata(MetaDataItem item)
    {
        var bytes = new[] {(byte) item.Kind.GetHashCode()};
        return item.Kind switch
        {
            (byte) MetaDataKind.Urn => bytes.Concat(UTF8.GetBytes(((Urn) item.Value).Value)).ToArray(),
            (byte) MetaDataKind.PropertyDescriptor => bytes.Append(((PropertyDescriptor) item.Value).Type)
                .Concat(UTF8.GetBytes(((PropertyDescriptor) item.Value).Urn.Value)).ToArray(),
            (byte) MetaDataKind.FirstDataPointTime => bytes.Concat(GetBytes(((TimeSpan) item.Value).Ticks)).ToArray(),
            (byte) MetaDataKind.LastDataPointTime => bytes.Concat(GetBytes(((TimeSpan) item.Value).Ticks)).ToArray(),
            (byte) MetaDataKind.DataPointsCount => bytes.Concat(GetBytes((long) item.Value)).ToArray(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public MetaDataItem DecodeMetadata(byte[] bytes)
    {
        var key = bytes[0];
        return key switch
        {
            (byte) MetaDataKind.Urn => Urn(BuildUrn(UTF8.GetString(bytes[1..]))),
            (byte) MetaDataKind.PropertyDescriptor => PropertyDescription(BuildUrn(UTF8.GetString(bytes[2..])),
                bytes[1]),
            (byte) MetaDataKind.FirstDataPointTime => FirstDataItemPointTime(TimeSpan.FromTicks(ToInt64(bytes[1..]))),
            (byte) MetaDataKind.LastDataPointTime => LastDataItemPointTime(TimeSpan.FromTicks(ToInt64(bytes[1..]))),
            (byte) MetaDataKind.DataPointsCount => DataPointsCount(ToInt64(bytes[1..])),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public byte[] EncodeDataPoint(RecordsDataPoint dataPoint, Dictionary<PropertyDescriptor, byte> propertyDescriptor,
        TimeSpan prevTime)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(dataPoint.At.Ticks);
        bw.Write(dataPoint.Id);

        foreach (var (descriptor, _) in propertyDescriptor)
        {
            var value = dataPoint.ValuesIndex.GetValueOrDefault(descriptor.Urn, null);

            WriteValue(bw, descriptor, value);
        }

        bw.Flush();
        return ms.ToArray();
    }

    public RecordsDataPoint DecodeDataPoint(byte[] bytes, PropertyDescriptor[] propertyDescriptors, TimeSpan prevTime)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        var at = br.ReadInt64();
        var id = br.ReadInt64();
        var dataPointValues = propertyDescriptors
            .Select(d =>
                new DataPointValue(
                    Urn: d.Urn,
                    Type: (FieldType) d.Type,
                    Value: GetObject(d, br))).ToArray();

        return new RecordsDataPoint(id, TimeSpan.FromTicks(at), dataPointValues);
    }

    private static object GetObject(PropertyDescriptor descriptor, BinaryReader br)
    {
        var isEndOfStream = br.BaseStream.IsAtTheEnd();
        return (FieldType) descriptor.Type switch
        {
            FieldType.String => isEndOfStream ? "" : br.ReadString(),
            FieldType.Enum => isEndOfStream? "" : br.ReadString(),
            FieldType.Float => isEndOfStream? float.NaN :br.ReadSingle(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static void WriteValue(BinaryWriter bw, PropertyDescriptor descriptor, object value)
    {
        switch ((FieldType) descriptor.Type)
        {
            case FieldType.String:
            case FieldType.Enum:
                bw.Write((string) value ?? string.Empty);
                break;
            case FieldType.Float:
                bw.Write(value != null ? (float) value : float.NaN);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}