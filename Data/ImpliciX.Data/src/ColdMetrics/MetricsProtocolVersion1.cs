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


namespace ImpliciX.Data.ColdMetrics;

internal static class ProtocolFactory
{
    public static IProtocol<MetricsDataPoint> Create(byte version) => version switch
    {
        1 => new MetricsProtocolVersion1(),
        _ => throw new ArgumentOutOfRangeException(nameof(version), version, "Protocol version not supported")
    };
}

internal sealed class MetricsProtocolVersion1 : IProtocol<MetricsDataPoint>
{
    public byte Version { get; } = 1;
    private byte[] FixedMetaDataItems { get; }
    public ushort MaxNumberOfPropertiesPerDataPoint { get; }
    private ushort NumberOfPointers { get; } = 128;
    public ushort HeaderSize { get; }
    public ushort HeaderOffset { get; }
    public int ContentOffset => HeaderOffset + HeaderSize;
    public MetricsProtocolVersion1()
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
            (byte) MetaDataKind.PropertyDescriptor => bytes.Concat(UTF8.GetBytes(((PropertyDescriptor) item.Value).Urn.Value)).ToArray(),
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
            (byte) MetaDataKind.PropertyDescriptor => PropertyDescription(BuildUrn(UTF8.GetString(bytes[1..]))),
            (byte) MetaDataKind.FirstDataPointTime => FirstDataItemPointTime(TimeSpan.FromTicks(ToInt64(bytes[1..]))),
            (byte) MetaDataKind.LastDataPointTime => LastDataItemPointTime(TimeSpan.FromTicks(ToInt64(bytes[1..]))),
            (byte) MetaDataKind.DataPointsCount => DataPointsCount(ToInt64(bytes[1..])),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public byte[] EncodeDataPoint(MetricsDataPoint dataPoint, Dictionary<PropertyDescriptor, byte> propertyDescriptor,
        TimeSpan prevTime)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write7BitEncodedInt64(DeltaTimeEncode(dataPoint.At, prevTime));
        bw.Write7BitEncodedInt64(DeltaTimeEncode(dataPoint.SampleStartTime, dataPoint.At));
        bw.Write7BitEncodedInt64(DeltaTimeEncode(dataPoint.SampleEndTime, dataPoint.At));

        var bytes = new byte[propertyDescriptor.Count * sizeof(float)];

        for (var i = 0; i < propertyDescriptor.Count; i += 1)
        {
            GetBytes(float.NaN).CopyTo(bytes, i * 4);
        }
        var indexUrn = propertyDescriptor.ToDictionary(it=>it.Key.Urn, it=>it.Value);
        foreach (var (urn, value) in dataPoint.Values)
        {
            if (!indexUrn.ContainsKey(urn))
                throw new Exception($"Urn {urn} is not indexed");

            var idx = indexUrn[urn];
            var offset = idx * sizeof(float);
            var getBytes = Array.Empty<byte>();
            try
            {
                getBytes = GetBytes(value);
                getBytes.CopyTo(bytes, offset);
            }
            catch (Exception ex)
            {
                var contextInfo =
                    $"\n---Context:\nBuffer.Length={bytes.Length}\ndataPoint.Values.Length={dataPoint.Values.Length}\nidx={idx}\noffset={offset}";
                var bufferContent = $"bufferContent={BitConverter.ToString(bytes)}";
                var getBytesInfo = $"GetBytes={BitConverter.ToString(getBytes)}";
                var dataPointInfo = $"DataPointInfo: {urn} = {value}";
                var indexUrnInfo =
                    propertyDescriptor.Aggregate("IndexUrnInfo:", (current, it) => current + $"({it.Key},{it.Value}); ");
                var innerMsg = $"\n---Initial Ex:\n{ex.Message}";
                var msg =
                    $"{contextInfo}\n{bufferContent}\n{getBytesInfo}\n{dataPointInfo}\n{indexUrnInfo}\n{innerMsg}";
                throw new InvalidOperationException(msg, ex);
            }
        }

        bw.Write(bytes);
        bw.Flush();
        return ms.ToArray();
    }

    private static long DeltaTimeEncode(TimeSpan t1, TimeSpan t2) =>
        (long) t1.TotalMilliseconds - (long) t2.TotalMilliseconds;

    public MetricsDataPoint DecodeDataPoint(byte[] bytes, PropertyDescriptor[] propertyDescriptors, TimeSpan prevTime)
    {
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms);
        var at = DeltaTimeDecode(prevTime, br.Read7BitEncodedInt64());
        var sampleStartTime = DeltaTimeDecode(at, br.Read7BitEncodedInt64());
        var sampleEndTime = DeltaTimeDecode(at, br.Read7BitEncodedInt64());
        var rawValues = new List<DataPointValue>(propertyDescriptors.Length);
        var index = 0;
        while (!ms.IsAtTheEnd())
        {
            rawValues.Add(new DataPointValue(propertyDescriptors[index++].Urn, br.ReadSingle()));
        }

        return new MetricsDataPoint(
            At: at,
            Values: rawValues.Where(v => !float.IsNaN(v.Value)).ToArray(),
            SampleStartTime: sampleStartTime,
            SampleEndTime: sampleEndTime
        );
    }

    private static TimeSpan DeltaTimeDecode(TimeSpan at, long delta) => at + TimeSpan.FromMilliseconds(delta);
}