using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Metrics
{
    public class MetricsModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef) =>
            new MetricsModule(moduleName, rtDef.Metrics, rtDef.Module<MetricsModuleDefinition>().SnapshotInterval, rtDef.Options);

        public MetricsModule(string id, IMetric[] metrics,
            TimeSpan snapshotInterval,
            ApplicationOptions options) : base(id)
        {
            DefineModule(
                initDependencies: configurator => configurator.AddSettings<MetricsSettings>("Modules", Id),
                initResources: provider =>
                {
                    var clock = provider.GetService<IClock>();
                    var internalBus = provider.GetService<IEventBusWithFirewall>();
                    var metricsService = new MetricsService(snapshotInterval, clock.Now);
                    return new object[]
                    {
                        metricsService, internalBus,
                        new TimeSeriesDb(
                            Path.Combine(options.LocalStoragePath, "internal", "metrics"),
                            "computers", 
                            safeLoad:options.StartMode == StartMode.Safe)
                    };
                },
                createFeature: assets =>
                {
                    var metricsService = assets.Get<MetricsService>();
                    var tsReader = assets.Get<IReadTimeSeries>();
                    var tsWriter = assets.Get<IWriteTimeSeries>();
                    var internalBus = assets.Get<IEventBusWithFirewall>();

                    var features = DefineFeature()
                        .Handles<PropertiesChanged>(metricsService.HandlePropertiesChanged)
                        .Handles<SystemTicked>(metricsService.HandleSystemTicked)
                        .Create();

                    var supportedMetrics = metrics.Where(m => m.Kind != MetricKind.Communication).ToArray();
                    internalBus.Publish(metricsService.Initialize(supportedMetrics, tsReader, tsWriter));

                    return features;
                });
        }
    }
}