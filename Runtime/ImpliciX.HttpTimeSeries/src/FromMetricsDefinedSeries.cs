using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;

namespace ImpliciX.HttpTimeSeries;

public sealed class FromMetricsDefinedSeries : IDefinedSeries, IMetricInfoVisitor
{
  private readonly Dictionary<Urn,(HashSet<Urn>, TimeSpan)> _seriesRetentionByRootUrn = new ();
  private readonly Dictionary<Urn,Urn> _outputUrnByRootUrn = new ();

  public FromMetricsDefinedSeries(MetricInfoSet metricInfoSet)
  {
    metricInfoSet.Accept(this);
  }

  public Urn[] OutputUrns => _outputUrnByRootUrn.Keys.ToArray();
  public Urn[] RootUrns => _seriesRetentionByRootUrn.Keys.ToArray();

  public bool ContainsRootUrn(Urn pcGroup) => _seriesRetentionByRootUrn.ContainsKey(pcGroup);

  public (HashSet<Urn>, TimeSpan) StorablePropertiesForRoot(Urn pcGroup) => 
    _seriesRetentionByRootUrn.GetValueOrDefault(pcGroup, (new HashSet<Urn>(), TimeSpan.Zero));

  public Urn? RootUrnOf(Urn tsUrn) => _outputUrnByRootUrn.GetValueOrDefault(tsUrn);
  
  public void VisitGaugeInfo(GaugeInfo info)
    => info.StorageRetention.Tap(retention =>
    {
      _seriesRetentionByRootUrn[info.RootUrn] = (new HashSet<Urn> {info.RootUrn}, retention);
      _outputUrnByRootUrn[info.RootUrn] = info.RootUrn;
    });


  public void VisitAccumulatorInfo(AccumulatorInfo info)
    => info.StorageRetention.Tap(retention =>
    {
      _seriesRetentionByRootUrn[info.RootUrn] = (new HashSet<Urn> {info.AccumulatedValue, info.SamplesCount}, retention);
      _outputUrnByRootUrn[info.AccumulatedValue] = info.RootUrn;
      _outputUrnByRootUrn[info.SamplesCount] = info.RootUrn;
 
    });

  public void VisitVariationInfo(VariationInfo info)
    => info.StorageRetention.Tap(retention =>
    {
      _seriesRetentionByRootUrn[info.RootUrn] = (new HashSet<Urn> {info.RootUrn}, retention);
      _outputUrnByRootUrn[info.RootUrn] = info.RootUrn;
    });

  public void VisitStateMonitoringInfo(StateMonitoringInfo info)
    => info.StorageRetention.Tap(retention =>
    {
      var outputUrns = info.States.Values.Aggregate(new HashSet<Urn>(), (urns, state) =>
      {
        urns.Add(state.Occurrence);
        urns.Add(state.Duration);
        state.Accumulators.ForEach(acc =>
        {
          urns.Add(acc.AccumulatedValue);
          urns.Add(acc.SamplesCount);
        });
        state.Variations.ForEach(v => urns.Add(v.OutputUrn));
        return urns;
      });
      
      _seriesRetentionByRootUrn[info.RootUrn] = (outputUrns, retention);
      outputUrns.ForEach(urn=> _outputUrnByRootUrn[urn] = info.RootUrn);
      
 
    });

  public void VisitGroupInfo(GroupInfo info)
    => info.StorageRetention.Tap(retention =>
    {
      _seriesRetentionByRootUrn[info.RootUrn] = (new HashSet<Urn> {info.RootUrn}, retention);
      _outputUrnByRootUrn[info.RootUrn] = info.RootUrn;
      
 
    });

  public void VisitGroupInfoAccumulator(GroupInfoAccumulator info)
    => info.StorageRetention.Tap(retention =>
    {
      _seriesRetentionByRootUrn[info.RootUrn] = (info.GetOutputUrns().Cast<Urn>().ToHashSet(), retention);
      info.GetOutputUrns().ForEach(urn => _outputUrnByRootUrn[urn] = info.RootUrn);
    });

  public void VisitGroupInfoStateMonitoring(GroupInfoStateMonitoring info)
    => info.StorageRetention
      .Tap(retention =>
      {
        _seriesRetentionByRootUrn[info.RootUrn] = (info.GetOutputUrns().Cast<Urn>().ToHashSet(), retention);
        info.GetOutputUrns().ForEach(urn => _outputUrnByRootUrn[urn] = info.RootUrn);
      });

  
}