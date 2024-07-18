using System;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public interface IBrahmaPrivateEvent
    {
        DeviceNode DeviceNode { get; }
    }

    public abstract class BrahmaPrivateEvent : PrivateDomainEvent, IBrahmaPrivateEvent
    {
        protected BrahmaPrivateEvent(DeviceNode deviceNode, BurnerNode genericBurner) : base(Guid.NewGuid(),
            TimeSpan.Zero)
        {
            DeviceNode = deviceNode;
            GenericBurner = genericBurner;
        }

        public DeviceNode DeviceNode { get; }
        public BurnerNode GenericBurner { get; }

        private bool Equals(BrahmaPrivateEvent other)
        {
            return Equals(DeviceNode, other.DeviceNode) && Equals(GenericBurner, other.GenericBurner);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BrahmaPrivateEvent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((DeviceNode != null ? DeviceNode.GetHashCode() : 0) * 397) ^
                       (GenericBurner != null ? GenericBurner.GetHashCode() : 0);
            }
        }

        public static bool operator ==(BrahmaPrivateEvent left, BrahmaPrivateEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BrahmaPrivateEvent left, BrahmaPrivateEvent right)
        {
            return !Equals(left, right);
        }
    }

    public class NotFaultedDetected : BrahmaPrivateEvent
    {
        private NotFaultedDetected(DeviceNode deviceNode, BurnerNode genericBurner) : base(deviceNode, genericBurner)
        {
        }

        public static NotFaultedDetected Create(DeviceNode deviceNode, BurnerNode genericBurner) =>
            new NotFaultedDetected(deviceNode, genericBurner);
    }

    public class FaultedDetected : BrahmaPrivateEvent
    {
        private FaultedDetected(DeviceNode deviceNode, BurnerNode genericBurner) : base(deviceNode, genericBurner)
        {
        }

        public static FaultedDetected Create(DeviceNode deviceNode, BurnerNode genericBurner) =>
            new FaultedDetected(deviceNode, genericBurner);
    }
}