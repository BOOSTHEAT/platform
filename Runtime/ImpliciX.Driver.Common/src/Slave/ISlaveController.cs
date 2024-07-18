using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Driver.Common.Slave
{
    public interface ISlaveController
    {
        public DomainEvent[] Activate();
        public bool CanHandle(DomainEvent trigger);
        public DomainEvent[] HandleDomainEvent(DomainEvent trigger);
    }
}