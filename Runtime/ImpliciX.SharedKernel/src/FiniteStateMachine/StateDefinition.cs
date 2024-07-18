using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    public class StateDefinition<TState, TInput, TOutput> : IEquatable<StateDefinition<TState, TInput, TOutput>>
    {
        internal Option<TState> ParentState { get; }
        public bool IsInitialSubState { get; }
        internal TState Alias { get; }
        public Func<TInput, TOutput[]>[] OnStateFuncs { get; }
        internal Func<TInput, TOutput[]>[] OnEntryFuncs { get; }
        internal Func<TInput, TOutput[]>[] OnExitFuncs { get; }


        public StateDefinition(TState alias, Func<TInput, TOutput[]>[] onEntryFuncs = null, Func<TInput, TOutput[]>[] onExitFuncs = null, Func<TInput,TOutput[]>[] onStateFuncs=null)
        {
            Alias = alias;
            OnStateFuncs = onStateFuncs ?? new Func<TInput, TOutput[]>[] { };
            OnEntryFuncs = onEntryFuncs ?? new Func<TInput, TOutput[]>[] { };
            OnExitFuncs = onExitFuncs ?? new Func<TInput, TOutput[]>[] { };
            ParentState = Option<TState>.None();
        }

        public StateDefinition(TState alias, TState parentState, bool isInitialSubState = false, Func<TInput, TOutput[]>[] onEntryFuncs = null, Func<TInput, TOutput[]>[] onExitFuncs = null, Func<TInput, TOutput[]>[] onStateFuncs = null) : this(alias, onEntryFuncs, onExitFuncs, onStateFuncs)
        {
            ParentState = parentState;
            IsInitialSubState = isInitialSubState;
        }

        public bool Equals(StateDefinition<TState, TInput, TOutput> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EqualityComparer<TState>.Default.Equals(Alias, other.Alias);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StateDefinition<TState, TInput, TOutput>) obj);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<TState>.Default.GetHashCode(Alias);
        }
    }
}