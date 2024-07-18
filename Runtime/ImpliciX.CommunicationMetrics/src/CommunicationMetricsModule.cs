using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.CommunicationMetrics
{
    public class CommunicationMetricsModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new CommunicationMetricsModule(moduleName, rtDef.ModelDefinition, rtDef.Metrics);

        public CommunicationMetricsModule(string id, Assembly modelDefinition,  IMetric[] metricsDefinitions) : base(id)
        {
            DefineModule(
                initDependencies: configurator => configurator.AddSettings<CommunicationMetricsSettings>("Modules", Id),
                initResources: (provider) =>
                {
                    var settings = provider.GetSettings<CommunicationMetricsSettings>(Id);   
                    var eventBus = provider.GetService<IEventBusWithFirewall>();
                    var clock = provider.GetService<IClock>();
                    var modelFactory = new ModelFactory(new[] {modelDefinition});
                    var domainEventFactory = EventFactory.Create(modelFactory, clock.Now);
                    
                    var communicationMetrics = metricsDefinitions
                        .Where(def => def.Kind == MetricKind.Communication)
                        .Cast<Metric<AnalyticsCommunicationCountersNode>>()
                        .ToArray();
                        
                    return new object[]
                    {
                        new CommunicationMetricsService(communicationMetrics, domainEventFactory),
                        eventBus,
                        settings
                    };
                },
                createFeature: assets =>
                {
                    var service = assets.Get<CommunicationMetricsService>();
                    var features = DefineFeature()
                        .Handles<SlaveCommunicationOccured>(service.HandleSlaveCommunication, service.CanHandle)
                        .Handles<SystemTicked>(service.HandleSystemTicked,service.CanHandle)
                        .Create();
                    return features;
                });
        }
    }
}