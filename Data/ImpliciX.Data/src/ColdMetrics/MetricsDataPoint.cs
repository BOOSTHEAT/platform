#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.ColdMetrics;

public record MetricsDataPoint : IDataPoint
{
    public MetricsDataPoint(TimeSpan At, DataPointValue[] Values, TimeSpan SampleStartTime, TimeSpan SampleEndTime)
    {
        this.At = At;
        this.Values = Values;
        this.SampleStartTime = SampleStartTime;
        this.SampleEndTime = SampleEndTime;
        Urns = Values.Select(it => it.Urn).ToHashSet();
        PropertyDescriptors = Urns.Select(it=> new PropertyDescriptor(it, 0)).ToArray();
        ValuesCount = Values.Length;

    }

    public static MetricsDataPoint[] FromModel(DataModelValue<MetricValue>[] modelValues)
    {
        return modelValues
            .OrderBy(mv => mv.At)
            .GroupBy(mv => mv.At)
            .Select(g => new MetricsDataPoint(
                At: g.Key,
                Values: g.Select(it => new DataPointValue(it.Urn, it.Value.Value)).ToArray(),
                SampleStartTime: g.Min(it => it.Value.SamplingStartDate),
                SampleEndTime: g.Max(it => it.Value.SamplingEndDate))
            )
            .ToArray();
    }

    internal HashSet<Urn> Urns { get; }
    public PropertyDescriptor[] PropertyDescriptors { get; init; }
    public int ValuesCount { get; init; }
    public TimeSpan At { get; init; }
    public DataPointValue[] Values { get; init; }
    public TimeSpan SampleStartTime { get; init; }
    public TimeSpan SampleEndTime { get; init; }

    public void Deconstruct(out TimeSpan At, out DataPointValue[] Values, out TimeSpan SampleStartTime, out TimeSpan SampleEndTime)
    {
        At = this.At;
        Values = this.Values;
        SampleStartTime = this.SampleStartTime;
        SampleEndTime = this.SampleEndTime;
    }

    public IEnumerable<DataModelValue<MetricValue>> ToModelValues(HashSet<Urn> projections)
    {
        var selectedUrns = projections.Any() switch
        {
            true => projections,
            false => Urns
        };            
        
        var results = PropertyDescriptors
            .Select((p, idx) => (p.Urn, Values[idx]))
            .Where(p => selectedUrns.Contains(p.Urn))
            .Select(p => new DataModelValue<MetricValue>(
                p.Urn,
                new MetricValue(p.Item2.Value, SampleStartTime, SampleEndTime),
                At));
        
        return results;
    }
}

public record DataPointValue(Urn Urn, float Value);