using ImpliciX.Data.Metrics;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.Language;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.HttpTimeSeries;

public class HttpTimeSeriesModule : ImpliciXModule
{
  public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
    => new HttpTimeSeriesModule(moduleName, rtDef);

  private HttpTimeSeriesModule(string id, ApplicationRuntimeDefinition rtDef) : base(id)
  {
    DefineModule(
      initDependencies: configurator => configurator.AddSettings<HttpTimeSeriesSettings>("Modules", Id),
      initResources: provider =>
      {
        var definition = rtDef.Module<HttpTimeSeriesDefinition>();

        var metrics = definition.Metrics
          .Select(d => d.Builder.Build<Metric<MetricUrn>>())
          .Where(m => m.Kind != MetricKind.Communication)
          .ToArray();
        var seriesFromMetrics = new FromMetricsDefinedSeries(CreateMetricInfos.Execute(metrics, rtDef.StateMachines));
        
        var selfDefinedSeries = new SelfDefinedSeries(definition.TimeSeries);
        var combinedSeries = new CombinedSeries(seriesFromMetrics, selfDefinedSeries);

        var options = rtDef.Options;
        var safeLoad = rtDef.Options.StartMode == StartMode.Safe;

        var dataService = new DataService(combinedSeries, DbFactory);
        var api = new SimPodWebApi(dataService);
        var app = new WebApiServer("*", 5283, api);
        return new object[] {dataService, app};

        IMetricsDbRepository DbFactory(IDefinedSeries ds)
          => new ColdMetricsDbRepository(ds, Path.Combine(options.LocalStoragePath, "internal", "http-time-series"), safeLoad);
      },
      createFeature: assets =>
      {
        var service = assets.Get<IDataService>();
        return DefineFeature()
          .Handles<PropertiesChanged>(service.StoreSeries, service.CanHandle)
          .Create();
      });
  }
}

public class HttpTimeSeriesSettings
{
}