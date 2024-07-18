using System;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class SlaveRestarted : PublicDomainEvent
    {
        public static SlaveRestarted Create(DeviceNode deviceNode, TimeSpan at)
        {
            return new SlaveRestarted(deviceNode, at);
        }
        
        public DeviceNode DeviceNode { get; }

        private SlaveRestarted(DeviceNode deviceNode, TimeSpan at): 
            base(Guid.NewGuid(), at)
        {
            DeviceNode = deviceNode;
        }

        protected bool Equals(SlaveRestarted other)
        {
            return Equals(DeviceNode, other.DeviceNode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SlaveRestarted) obj);
        }

        public override int GetHashCode()
        {
            return (DeviceNode != null ? DeviceNode.GetHashCode() : 0);
        }

        public static bool operator ==(SlaveRestarted left, SlaveRestarted right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SlaveRestarted left, SlaveRestarted right)
        {
            return !Equals(left, right);
        }
    }
}