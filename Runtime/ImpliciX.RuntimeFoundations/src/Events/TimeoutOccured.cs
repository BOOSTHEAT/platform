using System;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class TimeoutOccured : PublicDomainEvent
    {

        public static TimeoutOccured Create(Urn timerUrn, TimeSpan at, Guid requestId)
        {
            return new TimeoutOccured(timerUrn, at, requestId);
        }

        public Urn TimerUrn { get; }
        public Guid RequestId { get; }

        private TimeoutOccured(Urn timerUrn, TimeSpan at, Guid requestId) : base(Guid.NewGuid(), at)
        {
            TimerUrn = timerUrn;
            RequestId = requestId;
        }

        protected bool Equals(TimeoutOccured other)
        {
            return Equals(TimerUrn, other.TimerUrn) && Equals(RequestId, other.RequestId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TimeoutOccured) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (1 * 397) ^ (TimerUrn != null ? TimerUrn.GetHashCode() : 0);
            }
        }
    }
}