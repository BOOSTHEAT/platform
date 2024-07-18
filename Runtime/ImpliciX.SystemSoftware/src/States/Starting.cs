using System;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SystemSoftware.States
{
    public class Starting: BaseState<Context>
    {
        private readonly SystemSoftwareModuleDefinition _moduleDefinition;
        private readonly IDomainEventFactory _domainEventFactory;

        public Starting(SystemSoftwareModuleDefinition moduleDefinition, IDomainEventFactory domainEventFactory ) : base(moduleDefinition,domainEventFactory)
        {
            _moduleDefinition = moduleDefinition;
            _domainEventFactory = domainEventFactory;
        }

        protected override DomainEvent[] OnEntry(Context context, DomainEvent @event)
        {
            Log.Information("[SystemSoftware] is starting");
            return  
                (   from releaseVersion in SoftwareVersion.FromString(context.CurrentReleaseManifest.Revision)
                    from pc in _domainEventFactory.NewEventResult(_moduleDefinition.ReleaseVersion, releaseVersion)
                    select new DomainEvent[] { pc, new StartingCompleted(TimeSpan.Zero) })
                .LogWhenError("[SystemSoftware] failed to publish the current release version. {@0}")
                .GetValueOrDefault(Array.Empty<DomainEvent>());
        }

        public override bool CanHandle(DomainEvent @event) => @event is StartingCompleted;
        

        public Transition<BaseState<Context>, (Context, DomainEvent)> WhenSoftwareVersionsGathered(Ready targetState)
        {
            return new Transition<BaseState<Context>, (Context context, DomainEvent @event)>(
                this, 
                targetState, 
                x=> x.@event is StartingCompleted);
        }

        protected override string GetStateName()
        {
            return nameof(Starting);
        }
    }

    public class StartingCompleted : PrivateDomainEvent
    {
        public StartingCompleted(TimeSpan at) : base(Guid.NewGuid(),  at)
        {
        }
    }
}