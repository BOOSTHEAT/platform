using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.HttpTimeSeries;

public interface IDataService
{
  IDefinedSeries DefinedSeries { get; }
  string[] OutputUrnsAsStrings { get; }

  bool CanHandle(
    PropertiesChanged pc
  );

  DomainEvent[] StoreSeries(
    PropertiesChanged pc
  );

  IEnumerable<TimeSeriesValue> ReadDbSeriesValues(
    Urn tsUrn,
    TimeSpan? from = null,
    TimeSpan? to = null
  );
}

internal sealed class DataService : IDisposable, IDataService
{
  private readonly IMetricsDbRepository _repository;

  public DataService(
    IDefinedSeries definedSeries,
    Func<IDefinedSeries, IMetricsDbRepository> repositoryFactory
  )
  {
    DefinedSeries = definedSeries;
    _repository = repositoryFactory(DefinedSeries);
  }

  public IDefinedSeries DefinedSeries { get; }
  public string[] OutputUrnsAsStrings => DefinedSeries.OutputUrns.Select(u => u.Value).ToArray();

  public bool CanHandle(
    PropertiesChanged pc
  )
  {
    return pc.Group != null && DefinedSeries.ContainsRootUrn(pc.Group);
  }

  public DomainEvent[] StoreSeries(
    PropertiesChanged pc
  )
  {
    var (storableProperties, _) = DefinedSeries.StorablePropertiesForRoot(pc.Group);

    var storableModelValues = pc.ModelValues
      .Where(mv => storableProperties.Contains(mv.Urn))
      .Select(AsMetric)
      .Traverse();

    storableModelValues.Tap(
      e => throw new InvalidOperationException(e.Message),
      values => _repository.WriteMany(
        pc.Group,
        values.ToArray()
      )
    );
    return Array.Empty<DomainEvent>();
  }

  private static Result<DataModelValue<MetricValue>> AsMetric(IDataModelValue dmv) =>
    dmv as DataModelValue<MetricValue> ?? dmv.ToFloat().Match(
      Result<DataModelValue<MetricValue>>.Create,
      f => Property<MetricValue>.Create(
        PropertyUrn<MetricValue>.Build(dmv.Urn),
        new MetricValue(dmv.ToFloat().Value, dmv.At, dmv.At),
        dmv.At
      ));

  public IEnumerable<TimeSeriesValue> ReadDbSeriesValues(
    Urn tsUrn,
    TimeSpan? from = null,
    TimeSpan? to = null
  )
  {
    _repository.ApplyRetentionPolicy();
    var rootUrn = DefinedSeries.RootUrnOf(tsUrn);
    if (rootUrn == null)
      return Enumerable.Empty<TimeSeriesValue>();
    return _repository.Read(
        rootUrn,
        new[] { tsUrn },
        from,
        to
      )
      .Select(
        dataMv => new TimeSeriesValue(
          dataMv.At,
          dataMv.Value.Value
        )
      );
  }


  public void Dispose()
  {
    _repository.Dispose();
  }
}