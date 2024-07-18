using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SystemSoftware.States
{
    
    public class Ready : BaseState<Context>
    {
        private readonly SystemSoftwareModuleDefinition _moduleDefinition;

        public Ready(SystemSoftwareModuleDefinition moduleDefinition, IDomainEventFactory domainEventFactory) : base(moduleDefinition,domainEventFactory)
        {
            _moduleDefinition = moduleDefinition;
        }

        protected override DomainEvent[] OnEntry(Context context, DomainEvent @event)
        {
            Log.Information("[SystemSoftware] is started. Update commands can be handled.");
            return base.OnEntry(context, @event);
        }


        public override bool CanHandle(DomainEvent @event)
        {
            return @event switch
            {
                CommandRequested cr => IsUpdateCommand(cr),
                _ => false
            };
        }

        public Transition<BaseState<Context>, (Context context, DomainEvent @event)> WhenGeneralUpdateCommandReceived(BaseState<Context> targetState) =>
            new Transition<BaseState<Context>, (Context context, DomainEvent @event)>(
                this, 
                targetState, 
                x=>IsUpdateCommand(x.@event));

        private bool IsUpdateCommand(DomainEvent @event)
        {
            return @event is CommandRequested cr && cr.Urn.Equals(_moduleDefinition.GeneralUpdateCommand.command);
        }

        protected override string GetStateName()
        {
            return nameof(Ready);
        }
    }
}