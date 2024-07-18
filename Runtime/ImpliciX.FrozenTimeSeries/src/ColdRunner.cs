using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.FrozenTimeSeries;

public sealed class ColdRunner : IDisposable
{
  private readonly IColdMetricsDb _db;
  private readonly HashSet<Urn> _filter;
  private readonly Dictionary<Urn, Counter> _counters;

  public ColdRunner(IEnumerable<Urn> urns, IColdMetricsDb db, long logPeriodInSeconds)
  {
    _filter = urns.ToHashSet();
    _db = db;
    var logPeriod = TimeSpan.FromSeconds(logPeriodInSeconds);
    _counters = _filter.ToDictionary(urn => urn, _ => new Counter(0L, TimeSpan.Zero, logPeriod));
  }

  public DomainEvent[] StoreSeries(PropertiesChanged pc)
  {
    if (IsDisposed) throw new ObjectDisposedException(nameof(ColdRunner));

    FilterPropertiesToMetrics(_filter, pc).Tap(pair =>
      {
        var (metricUrn, values) = pair;
        _db.WriteMany(metricUrn, values);
        var count = _counters[metricUrn].Inc(pc.At);
        if(count.ShouldLog)
          Log.Information("FrozenTimeSeries for {urn} stored {count} data points", metricUrn, count.Value);
      }
    );

    return Array.Empty<DomainEvent>();
  }

  public class Counter
  {
    private long _value;
    private TimeSpan _at;
    private readonly TimeSpan _threshold;

    public Counter(long value, TimeSpan at, TimeSpan threshold)
    {
      _value = value;
      _at = at;
      _threshold = threshold;
    }

    public (long Value,bool ShouldLog) Inc(TimeSpan currentTime)
    {
      var elapsed = _threshold.Ticks > 0 && currentTime - _at > _threshold;
      _value++;
      if(elapsed)
        _at = currentTime;
      return (_value,elapsed);
    }
  }

  public static Option<(Urn, DataModelValue<MetricValue>[])> FilterPropertiesToMetrics(
    HashSet<Urn> filter,
    PropertiesChanged pc
  )
  {
    if (!filter.Contains(pc.Group))
      return Option<(Urn, DataModelValue<MetricValue>[])>.None();

    var metricProperties = pc
      .ModelValues
      .SelectMany(ConvertPropertyToRecordableMetric)
      .ToArray();
    if (metricProperties.Length == 0)
      return Option<(Urn, DataModelValue<MetricValue>[])>.None();

    return (pc.Group, metricProperties);
  }

  public static IEnumerable<DataModelValue<MetricValue>> ConvertPropertyToRecordableMetric(IDataModelValue property)
  {
    if (property is DataModelValue<MetricValue> metricDmv)
      yield return metricDmv;
    else if (property.ModelValue() is IFloat floatValue)
      yield return Property<MetricValue>.Create(
        PropertyUrn<MetricValue>.Build(property.Urn.Value),
        new MetricValue(floatValue.ToFloat(), property.At, property.At),
        property.At
      );
  }

  public void Dispose()
  {
    if (IsDisposed) return;
    _db.Dispose();
    IsDisposed = true;
  }

  private bool IsDisposed { get; set; }
}