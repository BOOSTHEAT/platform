using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Modules
{
    public class ImpliciXFeatureDefinition
    {
        private ConcurrentDictionary<Type, List<EventHandlerDefinition>> Handlers { get; }

        public static ImpliciXFeatureDefinition DefineFeature() => new ImpliciXFeatureDefinition();

        private ImpliciXFeatureDefinition()
        {
            Handlers = new ConcurrentDictionary<Type, List<EventHandlerDefinition>>();
        }

        public ImpliciXFeatureDefinition Handles<TEvent>(DomainEventHandler<TEvent> domainEventHandler)
            where TEvent : DomainEvent
        {
            var definition = new EventHandlerDefinition(typeof(TEvent), (_) => true, domainEventHandler);
            AddHandler(definition);
            return this;
        }

        public ImpliciXFeatureDefinition Handles<TEvent>(DomainEventHandler<TEvent> domainEventHandler,
            Func<TEvent, bool> predicate) where TEvent : DomainEvent
        {
            var definition = new EventHandlerDefinition(typeof(TEvent), (evt) => predicate((TEvent) evt), domainEventHandler);
            AddHandler(definition);
            return this;
        }

        private void AddHandler(EventHandlerDefinition handlerDefinition) =>
            Handlers.AddOrUpdate(
                handlerDefinition.EventType,
                _ => new List<EventHandlerDefinition>() { handlerDefinition },
                (_, hds) =>
                {
                    hds.Add(handlerDefinition);
                    return hds;
                });

        public IImpliciXFeature Create() => new ImpliciXFeature(Handlers);
    }
}