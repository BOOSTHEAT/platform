using System;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class NotifyOnTimeoutRequested : PublicDomainEvent, IEquatable<NotifyOnTimeoutRequested>
    {
        public Urn TimerUrn { get; }
        
        private NotifyOnTimeoutRequested(Urn timerUrn, TimeSpan at) : base(Guid.NewGuid(), at)
        {
            TimerUrn = timerUrn;
        }

        public static NotifyOnTimeoutRequested Create(Urn timerUrn, TimeSpan at)
        {
            return  new NotifyOnTimeoutRequested(timerUrn,at);
        }

        public bool Equals(NotifyOnTimeoutRequested other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(TimerUrn, other.TimerUrn);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((NotifyOnTimeoutRequested) obj);
        }

        public override int GetHashCode()
        {
            return (TimerUrn != null ? TimerUrn.GetHashCode() : 0);
        }
    }
}