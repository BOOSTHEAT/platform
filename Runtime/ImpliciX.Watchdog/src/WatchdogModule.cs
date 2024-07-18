using System;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Watchdog
{
    public class WatchdogModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new WatchdogModule(moduleName, rtDef.Module<WatchdogModuleDefinition>());

        public WatchdogModule(string id, WatchdogModuleDefinition watchdogs) : base(id)
        {
            DefineModule(
                initDependencies: configurator => configurator.AddSettings<Settings>("Modules", Id),
                initResources: (provider) =>
                {
                    var internalBus = provider.GetService<IEventBusWithFirewall>();
                    var settings = provider.GetSettings<Settings>(Id);
                    var clock = provider.GetService<IClock>();
                    return new object[] { new Controller(settings, watchdogs, clock, RestartAppFunc(internalBus, clock)) };
                },
                createFeature: assets =>
                {
                    var controller = assets.Get<Controller>();
                    return DefineFeature()
                        .Handles<PropertiesChanged>(controller.HandlePropertiesChanged, controller.CanHandleProperties)
                        .Create();
                });
        }

        private static Action<CommandUrn<NoArg>> RestartAppFunc(IEventBusWithFirewall eventBusWithFirewall, IClock clock)
        {
            return (restartBoilerAppUrn) =>
            {
                Log.Warning($"WatchDog Timeout. Application will restart immediately");
                eventBusWithFirewall.Publish(CommandRequested.Create(restartBoilerAppUrn, default(NoArg), clock.Now()));
            };
        }
    }
}