using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.ViewModels;

public class MetricViewModel : NamedModel
{
    public MetricViewModel(IMetric metric) : base(metric.TargetUrn.Value)
    {
        _metric = metric;
    }

    public class Description
    {
        public Description(string name, MetricKind kind, Urn trigger)
        {
            Name = name;
            Details = $"{Format(kind)} of {trigger}";
        }

        public string Name { get; }
        public string Details { get; }
    }

    public override IEnumerable<string> CompanionUrns => new[] {Name, PrimaryTrigger};
    public Description Main => new Description(Name, _metric.Kind, PrimaryTrigger);
    public string SamplePeriod => Format(_metric.PublicationPeriod);

    public string StoragePeriod => _metric.StoragePolicy
        .Match(() => "None", p => Format(p.Duration, p.TimeUnit));

    public IEnumerable<Description> Inclusions =>
        _metric.SubMetricDefs.Select(smd => new Description(smd.SubMetricName, smd.MetricKind, smd.InputUrn));

    private readonly IMetric _metric;
    private string PrimaryTrigger => _metric.InputUrn.Value;

    private static string Format(MetricKind kind) => kind switch
    {
        MetricKind.Gauge => "Gauge",
        MetricKind.Communication => "Device Monitoring",
        MetricKind.State => "State Monitoring",
        MetricKind.SampleAccumulator => "Accumulator",
        MetricKind.Variation => "Variation",
        _ => throw new ArgumentException("unexpected metric kind")
    };

    private static string Format(TimeSpan ts) => Interpret(ts) switch
    {
        var (duration, unit) => Format(duration, unit)
    };

    private static (int, TimeUnit) Interpret(TimeSpan ts) =>
        TryInterpret(ts.Seconds, TimeUnit.Seconds)
        ?? TryInterpret(ts.Minutes, TimeUnit.Minutes)
        ?? TryInterpret(ts.Hours, TimeUnit.Hours)
        ?? TryInterpret(ts.Days, TimeUnit.Days)
        ?? throw new ArgumentException("Unexpected time span");

    private static (int, TimeUnit)? TryInterpret(int value, TimeUnit unit) => value > 0
        ? (value, unit)
        : null;

    private static string Format(int duration, TimeUnit unit) => $"{duration} {Format(unit)}{(duration > 1 ? "s" : "")}";

    private static string Format(TimeUnit unit) =>
        unit switch
        {
            TimeUnit.Years => "year",
            TimeUnit.Quarters => "quarter",
            TimeUnit.Months => "month",
            TimeUnit.Weeks => "week",
            TimeUnit.Days => "day",
            TimeUnit.Hours => "hour",
            TimeUnit.Minutes => "minute",
            TimeUnit.Seconds => "second",
            _ => throw new ArgumentException("Unexpected time unit")
        };
}