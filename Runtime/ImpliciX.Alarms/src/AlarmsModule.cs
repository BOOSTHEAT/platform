using System.Collections.Generic;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Alarms;
using ImpliciX.RuntimeFoundations;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Alarms
{
    public class AlarmsModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new AlarmsModule(moduleName, rtDef.Module<AlarmsModuleDefinition>().Alarms, rtDef.ModelDefinition);

        public AlarmsModule(string id, IEnumerable<Alarm> alarmDeclarations, Assembly modelDefinition) : base(id)
        {
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<AlarmSettings>("Modules", Id),
                initResources: provider =>
                {
                    var settings = provider.GetSettings<AlarmSettings>(Id);
                    var clock = provider.GetService<IClock>();
                    var eventBus = provider.GetService<IEventBusWithFirewall>();
                    var factory = new ModelFactory(modelDefinition);
                    var alarmsDefinitions = new AlarmsDefinitions(alarmDeclarations, settings);
                    var alarmsService = new AlarmsService(alarmsDefinitions, clock, factory);
                    return new object[] {alarmsService, eventBus};
                },
                createFeature: assets =>
                {
                    var eventBus = assets.Get<IEventBusWithFirewall>();
                    var alarmService = assets.Get<AlarmsService>();
                    var feature = DefineFeature()
                        .Handles(alarmService.HandleSlaveCommunicationOccured,
                            alarmService.CanHandleSlaveCommunicationOccured)
                        .Handles(alarmService.HandlePropertiesChanged, alarmService.CanHandlePropertiesChanged)
                        .Handles(alarmService.HandleCommandRequested, alarmService.CanHandleCommandRequested)
                        .Create();
                    eventBus.Publish(alarmService.ActivateAlarms);
                    return feature;
                });
        }
    }
}