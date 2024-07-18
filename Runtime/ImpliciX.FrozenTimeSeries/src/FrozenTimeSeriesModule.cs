using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Data.TimeSeries;
using ImpliciX.Language;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.FrozenTimeSeries;

public class FrozenTimeSeriesModule : ImpliciXModule
{
  public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
    => new FrozenTimeSeriesModule(moduleName, rtDef);

  public FrozenTimeSeriesModule(string id, ApplicationRuntimeDefinition rtDef) : base(id)
  {
    DefineModule(
      initDependencies: cfg => cfg.AddSettings<FrozenTimeSeriesSettings>("Modules", Id),
      initResources: provider =>
      {
        var settings = provider.GetSettings<FrozenTimeSeriesSettings>(Id);
        var options = rtDef.Options;
        var definition = rtDef.Module<FrozenTimeSeriesDefinition>();
        var ofInterest = definition
          .Metrics
          .Select(d => d.Builder.Build<Metric<MetricUrn>>().TargetUrn)
          .Concat(definition.TimeSeries.Select(ts => ts.Urn()))
          .ToArray();
        var safeLoad = rtDef.Options.StartMode == StartMode.Safe;
        var coldMetricDb = ColdMetricsDb.LoadOrCreate(
          ofInterest,
          Path.Combine(options.LocalStoragePath, "external", "metrics"),
          safeLoad,
          new OneCompressedFileByDayAndBySession<MetricsDataPoint>()
        );
        var coldRunner = new ColdRunner(ofInterest , coldMetricDb, settings.LogPeriodInSeconds);
        return new object[] { coldRunner, settings };
      },
      createFeature: assets =>
      {
        var coldRunner = assets.Get<ColdRunner>();
        return
          DefineFeature()
            .Handles<PropertiesChanged>(coldRunner.StoreSeries)
            .Create();
      });
  }
}

public class FrozenTimeSeriesSettings
{
  public long LogPeriodInSeconds { get; set; } = 10;
}