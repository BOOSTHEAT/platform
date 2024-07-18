using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    public class FSM<TState, TInput, TOutput>
    {
        private TState MachineInitialState { get; }
        private FSMDefinition<TState, TInput, TOutput> Definition { get; }

        public FSM(TState initialState, StateDefinition<TState, TInput, TOutput>[] states, Transition<TState, TInput>[] transitions)
        {
            MachineInitialState = initialState;
            Definition = new FSMDefinition<TState, TInput, TOutput>(states, transitions);
        }
        
        public (TState nextState, TOutput[] output) Activate(TInput input,Action<object> stateChanged=null)
        {
            var (nextState, functionToExecute) = Definition.EnterStateDefinition(MachineInitialState, Option<TState>.None());
            stateChanged?.Invoke(nextState);
            return (nextState, ExecuteFunctions(input, functionToExecute));
        }
        
        public (TState nextState, TOutput[] ouput) TransitionFrom(TState fromState, TInput input,Action<object> stateChanged=null)
        {
            var (nextState, canTransition) = DecideNextState(fromState, input);
            if (!canTransition) return OperateOnState(input, fromState);
            stateChanged?.Invoke(nextState);
            return OperateTransition(input, fromState, nextState);
        }
        
        public bool IsSubState(TState parent, TState child)
        {
            return Definition.IsSubState(parent, child);
        } 
        
        private (TState NextState, bool CanTransition) DecideNextState(TState fromState, TInput input)
        {
            var possibleTransitions = PossibleTransitions(fromState, input);
            if (possibleTransitions.Length == 0)
                return (fromState, false);
            var nextState = possibleTransitions[0].To;
            return (nextState, true);
        }
        
        private Transition<TState, TInput>[] PossibleTransitions(TState fromState, TInput input)
        {
            var composition = Definition.StatesToEnter(fromState);
            foreach (var state in composition)
            {
                var transitions = Definition.TransitionsFrom(state).Where(t => t.Condition(input)).ToArray();
                if (transitions.Any())
                    return transitions;
            }
            return new Transition<TState, TInput>[0];
        }

        private (TState nextState, TOutput[] ouput) OperateOnState(TInput input, TState state)
        {
            var funcsOnState = Definition.OnStateFunctions(state);
            return (state, ExecuteFunctions(input, funcsOnState));
        }

        private (TState nextState, TOutput[] output) OperateTransition(TInput input, TState fromState, TState toState)
        {
            var (nextState, functionToExecute) = Definition.Transition(fromState, toState);
            return (nextState, ExecuteFunctions(input, functionToExecute));
        }

        private TOutput[] ExecuteFunctions(TInput input, IEnumerable<Func<TInput, TOutput[]>> funcs) 
        {
            var result = new List<TOutput>();
            foreach (var func in funcs)
            {
                result.AddRange(func(input));
            }
            return result.ToArray();
        }
    }
}