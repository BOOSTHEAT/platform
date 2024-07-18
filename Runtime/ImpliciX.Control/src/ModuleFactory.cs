using System;
using System.Linq;
using ImpliciX.Control.DomainEvents;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;


namespace ImpliciX.Control
{
    public static class ModuleFactory
    {
        public static IImpliciXFeature CreateFeature(IEventBusWithFirewall bus, DomainEventFactory domainEventFactory, Func<IDomainEventFactory, ControlSystem> buildControlSystem)
        {
            var service = buildControlSystem(domainEventFactory);
            var buffer = new Helpers.PropertiesChangedBuffer();
            var feature = DefineFeature()
                .Handles<CommandRequested>(service.HandleCommandRequest, service.CanHandle)
                .Handles<TimeoutOccured>(service.HandleTimeoutOccured)
                .Handles<PropertiesChanged>(propertiesChanged =>
                {
                    buffer.ReceivedPropertiesChanged(propertiesChanged);
                    return Array.Empty<DomainEvent>();
                })
                .Handles<StateChanged>(service.HandleStateChanged)
                .Handles<SystemTicked>(ticked => service.HandlePropertiesChanged(buffer.ReleasePropertiesChanged()).Concat(service.HandleSystemTicked(ticked)).ToArray())
                .Create();
            bus.Publish(service.Activate());
            return feature;
        }
    }
}