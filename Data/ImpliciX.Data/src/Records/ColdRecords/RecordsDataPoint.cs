using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Records.ColdRecords;

public record RecordsDataPoint : IDataPoint
{
    public RecordsDataPoint(long id, TimeSpan At, DataPointValue[] Values)
    {
        this.Id = id;
        this.At = At;   
        this.Values = Values;
        PropertyDescriptors = Values.Select(it=> new PropertyDescriptor(it.Urn, (byte)it.Type)).ToArray();
        ValuesIndex = Values.ToDictionary(it=>it.Urn, it=>it.Value);
    }

    public long Id { get; }

    public Dictionary<Urn, object> ValuesIndex { get; set; }
    
    

    public static RecordsDataPoint FromSnapshot(Snapshot snapshot)
    {
        return new RecordsDataPoint(snapshot.Id, snapshot.At, snapshot.ColdOutput().Select(DataPointValue.FromModel).ToArray());
    }

    public PropertyDescriptor[] PropertyDescriptors { get; init; }
    public int ValuesCount { get; init; }
    public TimeSpan At { get; init; }
    public DataPointValue[] Values { get; init; }
}

public enum FieldType:byte
{
    Enum = 126,
    String = 127,
    Float = 128,
}

public record DataPointValue(Urn Urn, FieldType Type, object Value)
{
    public static DataPointValue FromModel(IDataModelValue modelValue)
    {
        var urn = modelValue.Urn;
        var (type, val) = modelValue.ModelValue() switch
        {
            Literal l => (FieldType.String, l.PublicValue()),
            IFloat f => (FieldType.Float, f.ToFloat()),
            Enum e => (FieldType.Enum, e.ToString()),
            _ => throw new Exception($"Unknown type {modelValue.GetType()}")
        };
        
        return new DataPointValue(urn, type, val);
    }
}