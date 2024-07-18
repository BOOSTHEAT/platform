using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Records;

public record Snapshot
{
    public Snapshot(long Id, Urn RecordUrn, IIMutableDataModelValue[] Values, TimeSpan At, Urn formUrn)
    {
        this.Id = Id;
        this.RecordUrn = RecordUrn;
        this.Values = ProcessValues(formUrn, Values);
        this.At = At;
    }
    
    public Snapshot(long Id, Urn RecordUrn, IIMutableDataModelValue[] Values, TimeSpan At)
    {
        this.Id = Id;
        this.RecordUrn = RecordUrn;
        this.Values = Values;
        this.At = At;
    }

    public IDataModelValue[] HistoryOutput(int index)
    {
        var newValues = new List<IDataModelValue>();
        var values = Values.Prepend(new DataModelValue<long>("_id", Id, At));
        foreach (var mv in values)
        {
            var newUrn = Urn.BuildUrn(RecordUrn,$"{index}",mv.Urn);
            newValues.Add(mv.WithUrn(newUrn));
        }
        return newValues.OfType<IDataModelValue>().ToArray();
        
    }
    
    public IDataModelValue[] ColdOutput()
    {
        return Values.OfType<IDataModelValue>().ToArray();
    }
    
    
    private static IIMutableDataModelValue[] ProcessValues(Urn formUrn, IIMutableDataModelValue[] values)
    {
        var newValues = new List<IDataModelValue>();
        foreach (var mv in values)
        {
            var token = mv.Urn.TryRemoveRoot(formUrn, out var part)?part:mv.Urn;
            newValues.Add(mv.WithUrn(token));
        }
        return newValues.OfType<IIMutableDataModelValue>().ToArray();
    }

    public long Id { get; }
    public Urn RecordUrn { get; init; }
    public IIMutableDataModelValue[] Values { get; init; }
    public TimeSpan At { get; init; }
}