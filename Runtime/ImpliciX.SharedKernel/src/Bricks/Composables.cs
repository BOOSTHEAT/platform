using System;

namespace ImpliciX.SharedKernel.Bricks
{
    public delegate DomainEvent[] DomainEventHandler<TRIGGER>(TRIGGER trigger) where TRIGGER:DomainEvent;


    public static class Composables
    {
        public static DomainEventHandler<TRIGGER> DomainEventHandler<TRIGGER>(Func<TRIGGER, DomainEvent[]> f) where TRIGGER : DomainEvent
            => (domainEvt) => f(domainEvt);
    }

    public static class ComposablesExt
    {
        public static DomainEvent[] Run<TRIGGER>(this DomainEventHandler<TRIGGER> domainEventHandler,TRIGGER trigger) where TRIGGER : DomainEvent
        {
            return domainEventHandler(trigger);
        }
    }
}
