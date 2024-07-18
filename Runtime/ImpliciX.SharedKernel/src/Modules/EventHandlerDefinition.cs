using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Modules
{
    public class EventHandlerDefinition
    {
        public EventHandlerDefinition(Type eventType, Func<DomainEvent, bool> predicate, dynamic service)
        {
            Id = Guid.NewGuid();
            EventType = eventType;
            CanExecute = predicate;
            Service = service;
        }

        public Guid Id { get; }
        public Type EventType { get; }
        public Func<DomainEvent, bool> CanExecute { get; }
        public dynamic Service { get; }
    }
}