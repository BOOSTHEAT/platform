using System.Runtime.CompilerServices;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.TimeCapsule;

public class HotRunner
{
  private readonly IReadTimeSeries _tsReader;
  private readonly IWriteTimeSeries _tsWriter;
  private readonly Func<TimeSpan> _clock;

  public HotRunner(
    IEnumerable<Metric<MetricUrn>> metrics,
    IEnumerable<(Urn, ChartXTimeSpan)> xSpans,
    IReadTimeSeries tsReader,
    IWriteTimeSeries tsWriter,
    IClock clock)
  {
    _tsReader = tsReader;
    _tsWriter = tsWriter;
    _clock = clock.Now;

    DefinedSeries = CreateSeriesDefinitions(metrics, xSpans);
    if(DefinedSeries.Count == 0)
      Log.Warning("TimeCapsule is configured with no series and could be removed.");
    DefinedSeriesUrns = DefinedSeries.Keys.ToHashSet();
    DefinedSeriesGroupsUrns = DefinedSeries.Values.Where(d => d.IsGroup).Select(d => d.Urn).ToHashSet();
    Log.Debug("TimeCapsule is configured with: {TrackedMetricsUrns}", DefinedSeries.Values);
  }

  private static Dictionary<Urn, SeriesDefinition> CreateSeriesDefinitions(IEnumerable<Metric<MetricUrn>> metrics, IEnumerable<(Urn, ChartXTimeSpan)> xSpans)
  {
    var flatMetrics = metrics
      .SelectMany(metric => metric.GroupPoliciesUrns.Keys.Select(urn => (urn,true)).Prepend((metric.TargetUrn, false)))
      .DistinctBy(x => x.Item1)
      .ToArray();

    var series =
      from span in xSpans
      let metric = GetMetricOf(span.Item1)
      let baseUrn = metric.Item1
      let isGroup = metric.Item2
      let def = new SeriesDefinition(baseUrn, span.Item2.Duration, span.Item2.TimeUnit, isGroup)
      where def.RetentionPolicy.Ticks > 0
      orderby def.RetentionPolicy descending
      select def;
    
    var seriesByUrn = series
      .DistinctBy(s => s.Urn)
      .ToDictionary(s => s.Urn, s => s);
    return seriesByUrn;

    (Urn Urn, bool IsGroup) GetMetricOf(Urn urn)
    {
      var candidates = flatMetrics!.Where(m => m.Item1 == urn || m.Item1.IsPartOf(urn)).ToArray();
      if(candidates.Any())
        return candidates.MaxBy(m => m.Item1.Value.Length);
      throw new ApplicationException($"{urn.Value} is used in timeline chart but has no associated metric");
    }
  }

  private HashSet<Urn> DefinedSeriesGroupsUrns { get; }

  private HashSet<Urn> DefinedSeriesUrns { get; }

  public Dictionary<Urn, SeriesDefinition> DefinedSeries { get; }

  public bool CanHandle(PropertiesChanged pc) => pc.Group != null && DefinedSeriesUrns.Contains(pc.Group);

  public TimeSeriesChanged[] PublishAllSeries() => EventTimeSeriesChanged(DefinedSeriesUrns.ToHashSet());

  public TimeSeriesChanged[] StoreSeries(PropertiesChanged pc)
  {
    Log.Verbose("TimeCapsule.Runner: Store metrics received properties {pc}", pc);
    var series = SeriesRetentionDefinition(pc);
    var storableModelValues = pc.ModelValues
      .Where(mv => series.ContainsKey(mv.Urn))
      .ToList();

    Log.Verbose("TimeCapsule.Runner: Write values {storable} is {series}", storableModelValues, series);
    _tsWriter.WriteMany(storableModelValues, series);

    return EventTimeSeriesChanged(new HashSet<Urn> { pc.Group });
  }

  private TimeSeriesChanged[] EventTimeSeriesChanged(HashSet<Urn> changedSeriesUrns)
  {
    var outcome = new List<TimeSeriesChanged>();
    var dbKeys = _tsReader.AllKeys().GetValueOrDefault(Array.Empty<Urn>()).ToHashSet();
    var changedSimpleSeriesUrns = changedSeriesUrns.Where(it => !DefinedSeriesGroupsUrns.Contains(it)).ToArray();
    var changedGroupedSeriesUrns = changedSeriesUrns.Where(it => DefinedSeriesGroupsUrns.Contains(it)).ToArray();

    _tsReader.ApplyRetentionPolicy();

    foreach (var definedSeriesUrn in changedSimpleSeriesUrns)
    {
      var urnsToRead = dbKeys
        .Where(k => definedSeriesUrn.IsPartOf(k) && !DefinedSeriesGroupsUrns.Any(gu => gu.IsPartOf(k)))
        .Prepend(definedSeriesUrn);
      ReadAndAdd(urnsToRead, definedSeriesUrn);
    }

    foreach (var definedSeriesUrn in changedGroupedSeriesUrns)
    {
      var urnsToRead = dbKeys.Where(k => definedSeriesUrn.IsPartOf(k)).Prepend(definedSeriesUrn);
      ReadAndAdd(urnsToRead, definedSeriesUrn);
    }

    return outcome.ToArray();

    void ReadAndAdd(IEnumerable<Urn> urnsToRead, Urn definedSeriesUrn)
    {
      var timeSeriesChanged = ReadDbSeries(urnsToRead, definedSeriesUrn);
      if (timeSeriesChanged.TimeSeries.Count > 0)
        outcome.Add(timeSeriesChanged);
    }
  }

  private TimeSeriesChanged ReadDbSeries(IEnumerable<Urn> tsUrns, Urn simpleMetricUrn)
  {
    var readResults = _tsReader
      .ReadMany(tsUrns, TimeSpan.Zero.Ticks, _clock().Ticks)
      .GetValueOrDefault(Array.Empty<DataModelValue<float>>());

    var ts = CreateTimeSeries(readResults);

    return TimeSeriesChanged.Create(simpleMetricUrn, new(ts), _clock());
  }

  private static Dictionary<Urn, HashSet<TimeSeriesValue>> CreateTimeSeries(DataModelValue<float>[] values)
  {
    var outcome = new Dictionary<Urn, HashSet<TimeSeriesValue>>();
    SideEffect.TryRun(() =>
    {
      foreach (var modelValue in values)
      {
        var timeSeriesValues = new TimeSeriesValue(modelValue.At, modelValue.Value);
        outcome.AddOrUpdate(modelValue.Urn, new HashSet<TimeSeriesValue> { timeSeriesValues }, set =>
        {
          set.Add(timeSeriesValues);
          return set;
        });
      }
    }, _ => { });

    return outcome;
  }

  private Dictionary<Urn, TimeSpan> SeriesRetentionDefinition(PropertiesChanged pc)
  {
    var series = new Dictionary<Urn, TimeSpan>();
    if (pc.Group != null && DefinedSeries.ContainsKey(pc.Group))
    {
      pc.ModelValues
        .Aggregate(series, (acc, it) =>
        {
          acc[it.Urn] = DefinedSeries[pc.Group].RetentionPolicy;
          return acc;
        });
    }
    else
    {
      pc.ModelValues
        .Where(it => DefinedSeries.ContainsKey(it.Urn))
        .Aggregate(series, (acc, it) =>
        {
          acc[it.Urn] = DefinedSeries[it.Urn].RetentionPolicy;
          return acc;
        });
    }

    return series;
  }

  public record SeriesDefinition(Urn Urn, int Duration, TimeUnit TimeUnit, bool IsGroup)
  {
    public TimeSpan RetentionPolicy { get; } = ToRetentionPolicy(Duration, TimeUnit);

    public override string ToString() => $"{Urn} over past {Duration} {TimeUnit}";

    private static TimeSpan ToRetentionPolicy(int duration, TimeUnit timeUnit) => timeUnit switch
    {
      TimeUnit.Milliseconds => TimeSpan.FromMilliseconds(duration),
      TimeUnit.Seconds => TimeSpan.FromSeconds(duration),
      TimeUnit.Minutes => TimeSpan.FromMinutes(duration),
      TimeUnit.Hours => TimeSpan.FromHours(duration),
      TimeUnit.Days => TimeSpan.FromDays(duration),
      TimeUnit.Weeks => TimeSpan.FromDays(7 * duration),
      TimeUnit.Months => TimeSpan.FromDays(duration * 365f / 12f),
      TimeUnit.Quarters => TimeSpan.FromDays(duration * 365f / 4f),
      TimeUnit.Years => TimeSpan.FromDays(duration * 365),
      _ => throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null)
    };
  }
}