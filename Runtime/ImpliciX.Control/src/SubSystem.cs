#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Control
{
    public class SubSystem<TState> : IImpliciXSystem where TState : Enum
    {
        private readonly FSM<TState, DomainEvent, DomainEvent> _fsm;
        private readonly SubSystemDefinition<TState> _subSystemDefinition;
        private readonly IExecutionEnvironment _executionEnvironment;
        private readonly IDomainEventFactory _eventFactory;
        private readonly AlwaysRoutine _alwaysRoutine;
        private readonly List<Urn> _authorizedCommandRequestedUrns;
        public string Id => _subSystemDefinition.ID;

        public TState CurrentState { get; private set; }

        public SubSystem(SubSystemDefinition<TState> subSystemDefinition, IExecutionEnvironment executionEnvironment,
            IDomainEventFactory eventFactory, Option<TState> initialState)
        {
            DefinitionProcessing.AddMetaDataToDefinition(subSystemDefinition);
            _subSystemDefinition = subSystemDefinition;
            _executionEnvironment = executionEnvironment;
            CurrentState = initialState.GetValueOrDefault(subSystemDefinition.InitialState);
            _eventFactory = eventFactory;
            _authorizedCommandRequestedUrns = FillAuthorizedCommandRequestedUrns();
            var fsmFactory = new FSMFactory(executionEnvironment, eventFactory);
            _fsm = fsmFactory.BuildFsm(CurrentState, subSystemDefinition);
            _alwaysRoutine = new AlwaysRoutine(subSystemDefinition.Always, executionEnvironment, eventFactory);
        }

        public DomainEvent[] Activate() =>
            (from activationEvent in _eventFactory.NewEventResult(_subSystemDefinition.SubSystemNode._activate,
                    default(NoArg))
                from activationCommand in SideEffect.SafeCast<CommandRequested>(activationEvent)
                select HandleDomainEvent(activationCommand)).GetValueOrDefault(Array.Empty<DomainEvent>());

        public DomainEvent[] HandleDomainEvent(DomainEvent @event)
        {
            var resultingEvents = @event switch
            {
                CommandRequested commandRequested => HandleCommandRequest(commandRequested),
                PropertiesChanged propertiesChanged => HandlePropertiesChanged(propertiesChanged),
                SystemTicked systemTicked => HandleSystemTicked(systemTicked),
                TimeoutOccured timeoutOccured => HandleTimeoutOccured(timeoutOccured),
                _ => Array.Empty<DomainEvent>()
            };
            return resultingEvents;
        }

        private DomainEvent[] HandleCommandRequest(CommandRequested requestEvent)
        {
            if (requestEvent.Urn.Equals(_subSystemDefinition.SubSystemNode._activate))
            {
                var (activationState, activationOutput) = _fsm.Activate(requestEvent);
                CurrentState = activationState;
                return activationOutput;
            }

            return IsCurrentStateConcernedByCommandRequested(requestEvent)
                ? TransitionFrom(requestEvent)
                : Array.Empty<DomainEvent>();
        }

        private DomainEvent[] HandleTimeoutOccured(TimeoutOccured timeoutOccured)
        {
            return
                IsCurrentStateConcernedByTimeoutOccured(timeoutOccured) && _executionEnvironment.CheckTimeoutRequestExists(timeoutOccured)
                    ? TransitionFrom(timeoutOccured)
                    : Array.Empty<DomainEvent>();
        }

        private DomainEvent[] HandlePropertiesChanged(PropertiesChanged propertiesChanged)
        {
            if (IsCurrentStateConcernedByPropertiesChanged(propertiesChanged))
            {
                var alwaysOutput = _alwaysRoutine.Execute(propertiesChanged);
                var (nextState, output) = _fsm.TransitionFrom(CurrentState, propertiesChanged);
                CurrentState = nextState;
                return alwaysOutput.Concat(output).ToArray();
            }

            return Array.Empty<DomainEvent>();
        }

        private DomainEvent[] HandleSystemTicked(SystemTicked systemTicked) =>
            IsCurrentStateConcernedBySystemTicked
                ? TransitionFrom(systemTicked)
                : Array.Empty<DomainEvent>();

        public bool CanHandle(CommandRequested commandRequested) =>
            _authorizedCommandRequestedUrns.Contains(commandRequested.Urn);



        private bool IsCurrentStateConcernedBySystemTicked =>
            DefinitionProcessing.IsConcernedBySystemTicked(_subSystemDefinition, CurrentState);

        private bool IsCurrentStateConcernedByPropertiesChanged(PropertiesChanged propertiesChanged) =>
            propertiesChanged.PropertiesUrns
                .Intersect(DefinitionProcessing.GetCurrentConcernedUrnsForPropertiesChanged(_subSystemDefinition,
                    CurrentState)).Any();

        private bool IsCurrentStateConcernedByTimeoutOccured(TimeoutOccured timeoutOccured) =>
            DefinitionProcessing.GetCurrentConcernedTimeoutUrns(_subSystemDefinition, CurrentState).Contains(timeoutOccured.TimerUrn);

        private bool IsCurrentStateConcernedByCommandRequested(CommandRequested commandRequested) =>
            DefinitionProcessing.GetCurrentConcernedCommandRequestedUrns(_subSystemDefinition, CurrentState).Contains(commandRequested.Urn);

        private DomainEvent[] TransitionFrom(DomainEvent @event)
        {
            var (nextState, output) = _fsm.TransitionFrom(CurrentState, @event);
            CurrentState = nextState;
            return output;
        }

        private List<Urn> FillAuthorizedCommandRequestedUrns()
        {
            var authorizedUrns = new List<Urn> { _subSystemDefinition.SubSystemNode._activate };
            foreach (var stateDefinition in _subSystemDefinition.StateDefinitionsFlattened.Values)
            {
                authorizedUrns.AddRange(
                    stateDefinition.Transitions._whenMessages.Select(whenMessage => whenMessage._urn));
            }

            return authorizedUrns;
        }
    }
}