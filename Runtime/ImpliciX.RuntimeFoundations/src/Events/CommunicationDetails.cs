using System;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class CommunicationDetails : IEquatable<CommunicationDetails>
    {
        public CommunicationDetails(ushort successCount, ushort failureCount)
        {
            SuccessCount = successCount;
            FailureCount = failureCount;
        }

        public ushort SuccessCount { get; }
        public ushort FailureCount { get; }
        
        public bool Equals(CommunicationDetails other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SuccessCount == other.SuccessCount && FailureCount == other.FailureCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((CommunicationDetails) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SuccessCount, FailureCount);
        }

        public static bool operator ==(CommunicationDetails left, CommunicationDetails right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CommunicationDetails left, CommunicationDetails right)
        {
            return !Equals(left, right);
        }

        public static CommunicationDetails operator + (CommunicationDetails left, CommunicationDetails right)
        {
            var s = left.SuccessCount + right.SuccessCount;
            var f = left.FailureCount + right.FailureCount;
            return new CommunicationDetails((ushort)s, (ushort)f);
        }

        
        public override string ToString()
        {
            return $"{nameof(SuccessCount)}: {SuccessCount}, {nameof(FailureCount)}: {FailureCount}";
        }
    }
}