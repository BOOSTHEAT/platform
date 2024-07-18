using System;

namespace ImpliciX.SharedKernel.Bricks
{
    public abstract class DomainEvent
    {
        internal DomainEvent(Guid eventId, TimeSpan at)
        {
            EventId = eventId;
            At = at;
        }
        public Guid EventId { get; }
        public TimeSpan At { get; }
    }

    public abstract class PublicDomainEvent : DomainEvent
    {
        protected PublicDomainEvent(Guid eventId, TimeSpan at) : base(eventId, at)
        {
        }
    }

    public abstract class PrivateDomainEvent : DomainEvent
    {
        protected PrivateDomainEvent(Guid eventId, TimeSpan at) : base(eventId, at)
        {
        }
    }
}
