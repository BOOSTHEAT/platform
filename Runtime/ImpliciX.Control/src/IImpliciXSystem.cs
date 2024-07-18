using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control
{
    public interface IImpliciXSystem
    {
        DomainEvent[] HandleDomainEvent(DomainEvent @event);
        bool CanHandle(CommandRequested commandRequested);
        DomainEvent[] Activate();
    }
}