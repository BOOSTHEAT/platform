using ImpliciX.Language;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SystemSoftware.States
{
    public class Enabled: BaseState<Context>
    {
        protected override DomainEvent[] OnEntry(Context context, DomainEvent @event)
        {
            return base.OnEntry(context, @event);
        }

        protected override DomainEvent[] OnState(Context context, DomainEvent @event)
        {
            return base.OnState(context, @event);
        }
        
        public override bool CanHandle(DomainEvent @event)
        {
            return false;
        }

        protected override string GetStateName()
        {
            return nameof(Enabled);
        }

        public Enabled(SystemSoftwareModuleDefinition moduleDefinition, IDomainEventFactory domainEventFactory) : base(moduleDefinition, domainEventFactory)
        {
        }
    }
}