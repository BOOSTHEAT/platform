using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public class DummyPrivateDomainEvent : PrivateDomainEvent, IBHBoardPrivateEvent
    {
        public DummyPrivateDomainEvent(DeviceNode deviceNode) : base(Guid.NewGuid(), TimeSpan.Zero)
        {
            DeviceNode = deviceNode;
        }

        public DeviceNode DeviceNode { get; }

        protected bool Equals(DummyPrivateDomainEvent other)
        {
            return Equals(DeviceNode, other.DeviceNode);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DummyPrivateDomainEvent) obj);
        }

        public override int GetHashCode()
        {
            return (DeviceNode != null ? DeviceNode.GetHashCode() : 0);
        }
    }
}