using System;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    public interface IBHBoardPrivateEvent
    {
        DeviceNode DeviceNode { get; }
    }

    public abstract class BHBoardPrivateEvent : PrivateDomainEvent, IBHBoardPrivateEvent
    {
        protected BHBoardPrivateEvent(DeviceNode deviceNode) : base(Guid.NewGuid(), TimeSpan.Zero)
        {
            DeviceNode = deviceNode;
        }

        public DeviceNode DeviceNode { get; }

        private bool Equals(BHBoardPrivateEvent other) => Equals(DeviceNode, other.DeviceNode);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BHBoardPrivateEvent)obj);
        }

        public override int GetHashCode() => (DeviceNode != null ? DeviceNode.GetHashCode() : 0);
    }

    public class ExitBootloaderCommandSucceeded : BHBoardPrivateEvent
    {
        private ExitBootloaderCommandSucceeded(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static ExitBootloaderCommandSucceeded Create(DeviceNode deviceNode) =>
            new ExitBootloaderCommandSucceeded(deviceNode);
    }

    public class ProtocolErrorOccured : BHBoardPrivateEvent
    {
        private ProtocolErrorOccured(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static ProtocolErrorOccured Create(DeviceNode deviceNode) => new ProtocolErrorOccured(deviceNode);
    }

    public class ActivePartitionDetected : BHBoardPrivateEvent
    {
        public Partition Partition { get; }

        private ActivePartitionDetected(DeviceNode deviceNode, Partition partition) : base(deviceNode)
        {
            Partition = partition;
        }

        public static ActivePartitionDetected Create(DeviceNode deviceNode, Partition partition) =>
            new ActivePartitionDetected(deviceNode, partition);

        protected bool Equals(ActivePartitionDetected other)
        {
            return base.Equals(other) && Partition == other.Partition;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ActivePartitionDetected)obj);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), (int)Partition);
    }

    public class FirstFrameSuccessfullySent : BHBoardPrivateEvent
    {
        private FirstFrameSuccessfullySent(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static FirstFrameSuccessfullySent Create(DeviceNode deviceNode) =>
            new FirstFrameSuccessfullySent(deviceNode);
    }

    public class BoardUpdateRunningDetected : BHBoardPrivateEvent
    {
        private BoardUpdateRunningDetected(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static BoardUpdateRunningDetected Create(DeviceNode deviceNode) =>
            new BoardUpdateRunningDetected(deviceNode);
    }

    public class SendPreviousChunkSucceeded : BHBoardPrivateEvent
    {
        private SendPreviousChunkSucceeded(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static SendPreviousChunkSucceeded Create(DeviceNode deviceNode) =>
            new SendPreviousChunkSucceeded(deviceNode);
    }

    public class SendChunksFinished : BHBoardPrivateEvent
    {
        private SendChunksFinished(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static SendChunksFinished Create(DeviceNode deviceNode) => new SendChunksFinished(deviceNode);
    }

    public class SetActivePartitionSucceeded : BHBoardPrivateEvent
    {
        private SetActivePartitionSucceeded(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static SetActivePartitionSucceeded Create(DeviceNode deviceNode) =>
            new SetActivePartitionSucceeded(deviceNode);
    }

    public class RegulationEntered : BHBoardPrivateEvent
    {
        private RegulationEntered(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static RegulationEntered Create(DeviceNode deviceNode) => new RegulationEntered(deviceNode);
    }

    public class RegulationExited : BHBoardPrivateEvent
    {
        private RegulationExited(DeviceNode deviceNode) : base(deviceNode)
        {
        }

        public static RegulationExited Create(DeviceNode deviceNode) => new RegulationExited(deviceNode);
    }
}