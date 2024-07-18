using System;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public enum CommunicationStatus
    {
        Healthy,
        Error,
        Fatal
    }
    
    public class SlaveCommunicationOccured : PublicDomainEvent
    {
        public DeviceNode DeviceNode { get; }
        public CommunicationStatus CommunicationStatus { get; }
        public CommunicationDetails CommunicationDetails { get; }
        
        public static SlaveCommunicationOccured CreateHealthy(DeviceNode deviceNode, TimeSpan at, CommunicationDetails communicationDetails) =>
            new SlaveCommunicationOccured(deviceNode, at, CommunicationStatus.Healthy, communicationDetails);

        public static SlaveCommunicationOccured CreateError(DeviceNode deviceNode, TimeSpan at, CommunicationDetails communicationDetails) =>
            new SlaveCommunicationOccured(deviceNode, at, CommunicationStatus.Error, communicationDetails);

        public static SlaveCommunicationOccured CreateFatal(DeviceNode deviceNode, TimeSpan at, CommunicationDetails communicationDetails) =>
            new SlaveCommunicationOccured(deviceNode, at, CommunicationStatus.Fatal, communicationDetails);

        private SlaveCommunicationOccured(DeviceNode deviceNode, TimeSpan at,
            CommunicationStatus communicationStatus, CommunicationDetails communicationDetails) : base(Guid.NewGuid(), at)
        {
            DeviceNode = deviceNode;
            CommunicationStatus = communicationStatus;
            CommunicationDetails = communicationDetails??new CommunicationDetails(0,0);
        }

        protected bool Equals(SlaveCommunicationOccured other)
        {
            return Equals(DeviceNode, other.DeviceNode) 
                   && CommunicationStatus == other.CommunicationStatus 
                   && CommunicationDetails == other.CommunicationDetails;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SlaveCommunicationOccured) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DeviceNode, (int) CommunicationStatus, CommunicationDetails);
        }

        public static bool operator ==(SlaveCommunicationOccured left, SlaveCommunicationOccured right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SlaveCommunicationOccured left, SlaveCommunicationOccured right)
        {
            return !Equals(left, right);
        }
    }
}