using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Chronos
{
    public class ChronosModule : ImpliciXModule
    {
        public static ImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new ChronosModule(moduleName);

        public ChronosModule(string id) : base(id)
        {
          DefineModule(
                initDependencies: configurator => configurator.AddSettings<ChronosSettings>("Modules", Id),
                initResources: provider =>
                {
                    var eventBus = provider.GetService<IEventBusWithFirewall>();
                    var clock = provider.GetService<IClock>();
                    var timers = new ImpliciXTimers((evt) => eventBus.Publish(evt), clock);
                    var settings = provider.GetSettings<ChronosSettings>(Id);
                    return new object[] {timers, clock, settings, eventBus};
                }, createFeature: assets =>
                {
                    var timers = assets.Get<ImpliciXTimers>();
                    return DefineFeature()
                        .Handles<NotifyOnTimeoutRequested>(timers.HandleTimeoutRequest)
                        .Handles<PropertiesChanged>(timers.HandlePersistentChanged,
                            (changed) => changed.ModelValues.Any(mv => mv.ModelValue() is Duration))
                        .Create();
                });
            DefineSchedulingUnit(
                assets => schedulingUnit =>
                {
                    var clock = assets.Get<IClock>();
                    var origin = clock.Now();
                    var settings = assets.Get<ChronosSettings>();
                    var bus = assets.Get<IEventBusWithFirewall>();
                    var tickCounter = (uint) 1;
                    clock.SchedulePeriodic(
                        TimeSpan.FromMilliseconds(settings.BasePeriodMilliseconds),
                        () =>
                        {
                            var ticks = tickCounter++;
                            bus.Publish(SystemTicked.Create(origin, settings.BasePeriodMilliseconds, ticks));
                        });
                },
                _ => __ => { }
            );
        }
    }
}