using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Tools;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.TimeCapsule;

public class TimeCapsuleModule : ImpliciXModule
{
    public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
        => new TimeCapsuleModule(moduleName, rtDef);

    public TimeCapsuleModule(string id, ApplicationRuntimeDefinition rtDef) : base(id)
    {
        DefineModule(
            initDependencies: cfg => cfg.AddSettings<TimeCapsuleSettings>("Modules", Id),
            initResources: provider =>
            {
                var clock = provider.GetService<IClock>();
                var settings = provider.GetSettings<TimeCapsuleSettings>(Id);
                var options = rtDef.Options;
                var internalBus = provider.GetService<IEventBusWithFirewall>();
                var definition = rtDef.Module<TimeCapsuleDefinition>();
                if (definition.Metrics == null)
                    throw new ArgumentException("TimeCapsule is useless without Metrics");
                var metrics = definition.Metrics.Select(d => d.Builder.Build<Metric<MetricUrn>>()).ToArray();
                if (definition.UserInterface == null)
                    throw new ArgumentException("TimeCapsule is useless without GUI");
                var xSpans = UserInterfaceExplorer.GetInformationFor(definition.UserInterface).XSpans;
                var safeLoad = rtDef.Options.StartMode == StartMode.Safe;
                
                var hotDb =  new TimeSeriesDb(Path.Combine(options.LocalStoragePath, "internal", "time-capsule"), "metrics", safeLoad);
                var hotRunner = new HotRunner(metrics, xSpans, hotDb, hotDb, clock);
                return new object[] {internalBus, hotRunner, settings, clock, hotDb };
            },
            createFeature: assets =>
            {
                var hotRunner = assets.Get<HotRunner>();
                return 
                    DefineFeature()
                        .Handles<PropertiesChanged>(hotRunner.StoreSeries, hotRunner.CanHandle)
                        .Create();
            },
            onModuleStart: assets =>
            {
                var internalBus = assets.Get<IEventBusWithFirewall>();
                var hotRunner = assets.Get<HotRunner>();
                internalBus.Publish(hotRunner.PublishAllSeries());
            });
    }
}

public class TimeCapsuleSettings
{ 
}