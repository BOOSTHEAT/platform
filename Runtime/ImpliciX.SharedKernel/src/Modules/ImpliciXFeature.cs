using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Modules
{
    public class ImpliciXFeature : IImpliciXFeature
    {
        private readonly ConcurrentDictionary<Type, List<EventHandlerDefinition>> _handlers;

        public ImpliciXFeature(ConcurrentDictionary<Type, List<EventHandlerDefinition>> handlers)
        {
            _handlers = handlers;
        }

        public Type[] SupportedEvents => _handlers.Keys.ToArray();

        public bool CanExecute(DomainEvent @event) =>
            HandlerDefinitionsFor(@event.GetType()).Any(hd => hd.CanExecute(@event));

        public DomainEvent[] Execute(DomainEvent @event)
        {
            var executableHandlers = HandlerDefinitionsFor(@event.GetType()).Where(hd => hd.CanExecute(@event));
            return ExecuteHandler(executableHandlers, @event);
        }

        private List<EventHandlerDefinition> HandlerDefinitionsFor(Type eventType)
        {
            if (_handlers.TryGetValue(eventType, out var result))
                return result;
            Log.Error("{@Event} : This event has no handler.", eventType);
            return new List<EventHandlerDefinition>();
        }

        private DomainEvent[] ExecuteHandler(IEnumerable<EventHandlerDefinition> handlerDefinitions, DomainEvent @event) =>
            handlerDefinitions.SelectMany(hd =>
            {
                var handler = hd.Service;

                if (handler.DynamicInvoke(@event) is IEnumerable<DomainEvent> resultingEvents)
                {
                    return resultingEvents.ToArray();
                }

                return Array.Empty<DomainEvent>();
            }).ToArray();
    }
}