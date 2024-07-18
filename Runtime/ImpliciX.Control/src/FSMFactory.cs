using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Control
{
    internal class FSMFactory
    {
        private readonly IExecutionEnvironment _executionEnvironment;
        private readonly IDomainEventFactory _eventFactory;

        public FSMFactory(IExecutionEnvironment executionEnvironment, IDomainEventFactory eventFactory)
        {
            _executionEnvironment = executionEnvironment;
            _eventFactory = eventFactory;
        }

        public FSM<TState, DomainEvent, DomainEvent> BuildFsm<TState>(TState initialState,
            SubSystemDefinition<TState> subSystemDefinition) where TState : Enum
        {
            var transitions = new List<Transition<TState, DomainEvent>>();
            var states = new List<StateDefinition<TState, DomainEvent, DomainEvent>>();

            foreach (var stateDefinition in subSystemDefinition.StateDefinitionsFlattened.Values)
            {
                var efb = new EventFuncsBuilder(_executionEnvironment, _eventFactory);
                SetupPublishCurrentState(subSystemDefinition, efb, stateDefinition);
                efb.Setup(stateDefinition.OnEntry);
                efb.Setup(stateDefinition.OnState);
                efb.Setup(stateDefinition.OnExit);
                SetupTransitions(stateDefinition, transitions);

                states.Add(
                    stateDefinition._parentState.Match(
                        () => new StateDefinition<TState, DomainEvent, DomainEvent>(
                            stateDefinition._stateToConfigure, efb.OnEntryFuncs, efb.OnExitFuncs,
                            efb.OnStateFuncs),
                        parentState => new StateDefinition<TState, DomainEvent, DomainEvent>(
                            stateDefinition._stateToConfigure, parentState,
                            stateDefinition._isInitialSubState, efb.OnEntryFuncs, efb.OnExitFuncs,
                            efb.OnStateFuncs))
                );
            }

            return new FSM<TState, DomainEvent, DomainEvent>(initialState, states.ToArray(),
                transitions.ToArray());
        }

        private void SetupTransitions<TState>(Define<TState> stateDefinition,
            List<Transition<TState, DomainEvent>> transitions)
            where TState : Enum
        {
            foreach (var whenMessage in stateDefinition.Transitions._whenMessages)
            {
                transitions.Add(
                    new Transition<TState, DomainEvent>(stateDefinition._stateToConfigure,
                        whenMessage._state,
                        input => input is CommandRequested &&
                                 _eventFactory.NewEventResult(whenMessage._urn, whenMessage._value).Equals(input))
                );
            }

            foreach (var whenTimeout in stateDefinition.Transitions._whenTimeouts)
            {
                transitions.Add(
                    new Transition<TState, DomainEvent>(
                        stateDefinition._stateToConfigure,
                        whenTimeout._state,
                        (input) => input is TimeoutOccured timeoutOccured &&
                                   timeoutOccured.TimerUrn == whenTimeout._timerUrn));
            }

            transitions.AddRange(from whenCondition in stateDefinition.Transitions._whenConditions
                let fromState = stateDefinition._stateToConfigure
                let toState = whenCondition._target
                let conditionContext = new Helpers.ConditionContext(_executionEnvironment.GetProperty, whenCondition.Definition)
                select new Transition<TState, DomainEvent>(fromState, toState, _ => conditionContext.Execute()));
        }

        private void SetupPublishCurrentState<TState>(SubSystemDefinition<TState> subSystemDefinition,
            EventFuncsBuilder efb,
            Define<TState> stateDefinition) where TState : Enum
        {
            if (stateDefinition._isLeaf)
            {
                efb.AddOnEntry(_ =>
                    _eventFactory.NewEventResult(subSystemDefinition.StateUrn,
                        SubsystemState.Create(stateDefinition._stateToConfigure)));
                efb.AddOnEntry(_ => _eventFactory.NewEventResult(subSystemDefinition.ID,
                    CreateStatesChainFromCurrentState(stateDefinition, subSystemDefinition)));
            }
        }

        private static EnumSequence CreateStatesChainFromCurrentState<TState>(Define<TState> stateDefinition,
            SubSystemDefinition<TState> subSystemDefinition) where TState : Enum
        {
            var states = DefinitionProcessing.GetChainOfStates(subSystemDefinition, stateDefinition._stateToConfigure);
            return EnumSequence.Create(states.Cast<Enum>().ToArray());
        }
    }
}