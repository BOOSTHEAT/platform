#nullable enable
using System;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SystemSoftware.States
{
    public abstract class BaseState<CONTEXT>
    {
        private readonly SystemSoftwareModuleDefinition _moduleDefinition;
        private readonly IDomainEventFactory _domainEventFactory;

        public BaseState(SystemSoftwareModuleDefinition moduleDefinition, IDomainEventFactory domainEventFactory)
        {
            ParentState = Option<BaseState<CONTEXT>>.None();
            _moduleDefinition = moduleDefinition;
            _domainEventFactory = domainEventFactory;
        }


        public StateDefinition<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent> Define() =>
            ParentState.Match(
                DefineState,
                parentState => DefineSubState(parentState, IsInitial)
            );


        private StateDefinition<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent> DefineState() =>
            new StateDefinition<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent>(this,
                onEntryFuncs: new Func<(CONTEXT, DomainEvent), DomainEvent[]>[] { OnEntryEnrichWithState },
                onExitFuncs: new Func<(CONTEXT, DomainEvent), DomainEvent[]>[] { x => OnExit(x.Item1, x.Item2) },
                onStateFuncs: new Func<(CONTEXT, DomainEvent), DomainEvent[]>[] { x => OnState(x.Item1, x.Item2) });

        private StateDefinition<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent> DefineSubState(BaseState<CONTEXT> parentState, bool isInitialSubState = false)
        {
            return new StateDefinition<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent>(this, parentState, isInitialSubState,
                onEntryFuncs: new Func<(CONTEXT, DomainEvent), DomainEvent[]>[] { OnEntryEnrichWithState },
                onExitFuncs: new Func<(CONTEXT, DomainEvent), DomainEvent[]>[] { x => OnExit(x.Item1, x.Item2) },
                onStateFuncs: new Func<(CONTEXT, DomainEvent), DomainEvent[]>[] { x => OnState(x.Item1, x.Item2) });
        }

        DomainEvent[] OnEntryEnrichWithState((CONTEXT, DomainEvent) x)
        {
            var StateNameEvent = from stateName in GetUpdateState() from ev in _domainEventFactory.NewEventResult(_moduleDefinition.UpdateState, stateName) select ev;
            var onEntryResult = OnEntry(x.Item1, x.Item2);
            return onEntryResult.Append(StateNameEvent.Value).ToArray();
        }

        private Result<UpdateState> GetUpdateState()
        {
            return Enum.Parse<UpdateState>(GetStateName());
        }


        protected virtual DomainEvent[] OnEntry(CONTEXT context, DomainEvent @event) => Array.Empty<DomainEvent>();

        protected virtual DomainEvent[] OnState(CONTEXT context, DomainEvent @event) => Array.Empty<DomainEvent>();

        protected virtual DomainEvent[] OnExit(CONTEXT context, DomainEvent @event) => Array.Empty<DomainEvent>();

        public bool IsInitial { get; set; }
        public Option<BaseState<CONTEXT>> ParentState { get; set; }

        public abstract bool CanHandle(DomainEvent @event);
        protected abstract string GetStateName();
    }

    public static class BaseStateExtensions
    {
        public static T AsSubStateOf<T, CONTEXT>(this BaseState<CONTEXT> @this, BaseState<CONTEXT> parentState, bool isInitialSubState = false) where T : BaseState<CONTEXT>
        {
            @this.ParentState = Option<BaseState<CONTEXT>>.Some(parentState);
            @this.IsInitial = isInitialSubState;
            return (T)@this;
        }
    }
}