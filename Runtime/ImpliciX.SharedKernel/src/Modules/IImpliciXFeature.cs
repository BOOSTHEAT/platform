using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Modules
{
    public interface IImpliciXFeature
    {
        public Type[] SupportedEvents { get; }
        public bool CanExecute(DomainEvent @event);
        public DomainEvent[] Execute(DomainEvent @event);
    }
}