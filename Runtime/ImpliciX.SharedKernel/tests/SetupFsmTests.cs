using System;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SharedKernel.Tests
{
    public class SetupFsmTests
    {
        public enum MyState
        {
            A,
            B,
            BA,
            BB,
            BC,
            BCA,
            BAA,
            C,
            D,
            DCB,
            DCA,
            DC,
            DCAA
        }

        protected StateDefinition<MyState, int, int> CreateStateDefinition(MyState state,
            Func<int, int[]>[] onEntryFuncs = null, Func<int, int[]>[] onExitFuncs = null,
            Func<int, int[]>[] onStateFuncs = null)
            => new StateDefinition<MyState, int, int>(state, onEntryFuncs, onExitFuncs, onStateFuncs);

        protected StateDefinition<MyState, int, int> CreateStateDefinition(MyState state, MyState parentState,
            bool isInitialState, Func<int, int[]>[] onEntryFuncs = null, Func<int, int[]>[] onExitFuncs = null,
            Func<int, int[]>[] onStateFuncs = null)
            => new StateDefinition<MyState, int, int>(state, parentState, isInitialState, onEntryFuncs, onExitFuncs,
                onStateFuncs);

        protected StateDefinition<MyState, string, string> CreateStateDefinitionS(MyState state,
            Func<string, string[]>[] onEntryFuncs = null, Func<string, string[]>[] onExitFuncs = null,
            Func<string, string[]>[] onStateFuncs = null)
            => new StateDefinition<MyState, string, string>(state, onEntryFuncs, onExitFuncs, onStateFuncs);

        protected StateDefinition<MyState, string, string> CreateStateDefinitionS(MyState state, MyState parentState,
            bool isInitialState, Func<string, string[]>[] onEntryFuncs = null,
            Func<string, string[]>[] onExitFuncs = null, Func<string, string[]>[] onStateFuncs = null)
            => new StateDefinition<MyState, string, string>(state, parentState, isInitialState, onEntryFuncs,
                onExitFuncs, onStateFuncs);

        protected Transition<MyState, int> CreateTransition(MyState @from, MyState to, Func<int, bool> condition = null)
            => new Transition<MyState, int>(@from, to, condition);
        
        protected Transition<MyState, string> CreateTransitionS(MyState @from, MyState to, Func<string, bool> condition = null)
            => new Transition<MyState, string>(@from, to, condition);

        protected FSM<MyState, int, int> CreateFSM(MyState initialState, StateDefinition<MyState, int, int>[] states,
            Transition<MyState, int>[] transitions = null)
            => new FSM<MyState, int, int>(initialState, states, transitions ?? new Transition<MyState, int>[0]);

        protected FSM<MyState, string, string> CreateFSM(MyState initialState, StateDefinition<MyState, string, string>[] states,
            Transition<MyState, string>[] transitions = null)
            => new FSM<MyState, string, string>(initialState, states, transitions ?? new Transition<MyState, string>[0]);
    }
}