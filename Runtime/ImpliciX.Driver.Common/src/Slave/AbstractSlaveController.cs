using System.Collections.Generic;
using ImpliciX.Data;
using ImpliciX.Driver.Common.EventsProcessor;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.Driver.Common.Slave
{
    public abstract class AbstractSlaveController<TSlave, TState> : ISlaveController
        where TSlave : IBoardSlave
        where TState : struct
    {
        public DomainEvent[] Activate()
        {
            var (nextState, events) = _fsm.Activate(default);
            CurrentState = nextState;
            return events;
        }

        public virtual bool CanHandle(DomainEvent trigger)
        {
            return trigger switch
            {
                CommandRequested cr => IsEnabled() && (ControllersCommandsUrns.Contains(cr.Urn) || _boardSlave.IsConcernedByCommandRequested(cr.Urn)),
                PropertiesChanged pc => pc.ContainsAny(_boardSlave.SettingsUrns),
                SystemTicked _ => IsEnabled(),
                PrivateDomainEvent pe => CanHandlePrivateEvent(pe),
                TimeoutOccured to => IsEnabled() && TimersUrns.Contains(to.TimerUrn),
                _ => false,
            };
        }

        public DomainEvent[] HandleDomainEvent(DomainEvent trigger)
        {
            Debug.PreCondition(() => CurrentState.HasValue, () => "Current state has no value. Controller is not activated");
            if (trigger is PropertiesChanged propertiesChanged)
            {
                SettingsStorage.Store(_boardSlave.SettingsUrns, _driverStateKeeper, propertiesChanged);
            }

            var (nextState, events) = _fsm.TransitionFrom(CurrentState!.Value, trigger);
            CurrentState = nextState;
            return OutputEventsProcessor.MergeAndFilterPropertiesChanged(_driverStateKeeper, _domainEventFactory, events);
        }

        protected AbstractSlaveController(TSlave boardSlave, DomainEventFactory domainEventFactory, DriverStateKeeper driverStateKeeper, TState? currentState)
        {
            _boardSlave = boardSlave;
            _domainEventFactory = domainEventFactory;
            _driverStateKeeper = driverStateKeeper;
            CurrentState = currentState;
            Group = _boardSlave.DeviceNode.Urn.Plus(_boardSlave.Name);
        }

        protected abstract bool CanHandlePrivateEvent(PrivateDomainEvent privateDomainEvent);
        protected abstract bool IsEnabled();
        protected abstract HashSet<Urn> ControllersCommandsUrns { get; }
        protected abstract HashSet<Urn> TimersUrns { get; }
        public TState? CurrentState { get; protected set; }
        public Urn Group {get;}

        protected readonly TSlave _boardSlave;
        private readonly DomainEventFactory _domainEventFactory;
        private readonly DriverStateKeeper _driverStateKeeper;
        protected FSM<TState, DomainEvent, DomainEvent> _fsm;
    }
}