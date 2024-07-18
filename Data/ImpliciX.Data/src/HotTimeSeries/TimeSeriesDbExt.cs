using System;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.HotTimeSeries;

public static class TimeSeriesDbExt
{
    public static DataModelValue<float> FromBytes(byte[] bytes, Urn u)
    {
        var t = TimeSpan.FromTicks(BitConverter.ToInt64(bytes[..8]));
        var v = BitConverter.ToSingle(bytes[8..12]);
        return new DataModelValue<float>(u, v, t);
    }

    public static byte[] ToBytes(IDataModelValue v)
    {
        var bytes = new byte[12];
        BitConverter.GetBytes(v.At.Ticks).CopyTo(bytes, 0);
        if (v.ModelValue() is MetricValue mv)
            BitConverter.GetBytes(mv.Value).CopyTo(bytes, 8);
        else if (v.ModelValue() is float f)
            BitConverter.GetBytes(f).CopyTo(bytes, 8);
        else
            throw new NotSupportedException();
        return bytes;
    }
}