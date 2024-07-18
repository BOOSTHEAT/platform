using System;
using ImpliciX.Language.Model;
using static ImpliciX.Language.Model.Urn;


namespace ImpliciX.Data.ColdDb;

public record MetaDataPointer(long PointerOffset, uint ValueOffset, byte Kind, object Value, int Length);

public enum MetaDataKind : byte
{
    Urn = 1,
    PropertyDescriptor = 2,
    FirstDataPointTime = 3,
    DataPointsCount = 4,
    LastDataPointTime = 5
}
public class MetaDataItem
{
    public MetaDataItem(Byte kind, object value, bool isUnique, bool canUpdate)
    {
        Kind = kind;
        Value = value;
        IsUnique = isUnique;
        CanUpdate = canUpdate;
    }

    public Byte Kind { get; }
    public object Value { get; }
    public bool IsUnique { get; }
    public bool CanUpdate { get; }

    private bool Equals(MetaDataItem other)
    {
        return Kind == other.Kind && Equals(Value, other.Value) && IsUnique == other.IsUnique && CanUpdate == other.CanUpdate;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MetaDataItem) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) Kind, Value, IsUnique, CanUpdate);
    }
    public static MetaDataItem Urn(Urn urn) => new ((byte) MetaDataKind.Urn, urn, true, false);
    public static MetaDataItem DataPointsCount(long count) => new ((byte) MetaDataKind.DataPointsCount, count, true, true);
    public static MetaDataItem FirstDataItemPointTime(TimeSpan time) => new ((byte) MetaDataKind.FirstDataPointTime, time, true, false);
    public static MetaDataItem LastDataItemPointTime(TimeSpan time) => new ((byte) MetaDataKind.LastDataPointTime, time, true, true);

    public static MetaDataItem PropertyDescription(PropertyDescriptor descriptor) => 
        new ((byte) MetaDataKind.PropertyDescriptor, descriptor, false, false);
    
    public static MetaDataItem PropertyDescription(string urn, byte type = 0) => 
        new ((byte) MetaDataKind.PropertyDescriptor, new PropertyDescriptor(BuildUrn(urn), type), false, false);
}

public record PropertyDescriptor(Urn Urn, byte Type);

public interface IColdMetaData
{
    Urn Urn { get; init; }
    TimeSpan? FirstDataPointTime { get; init; }
    TimeSpan? LastDataPointTime { get; init; }
    long DataPointsCount { get; init; }
}

public record ColdMetaData(
    Urn? Urn = null,
    PropertyDescriptor[]? PropertyDescriptors = null,
    TimeSpan? FirstDataPointTime = null,
    TimeSpan? LastDataPointTime = null,
    long DataPointsCount = 0) : IColdMetaData;