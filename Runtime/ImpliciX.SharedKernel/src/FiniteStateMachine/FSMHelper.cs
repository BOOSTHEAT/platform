using System;
using System.Linq;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    public static class FSMHelper
    {
        public static Transition<TState, TInput> Transition<TState, TInput>(TState from, TState to, Func<TInput,bool> condition)
        {
            return new Transition<TState, TInput>(@from,to,condition);
        }
        
        public static WhenThen When(Func<DomainEvent, bool> when) => new WhenThen(when);

        public static Func<DomainEvent, DomainEvent[]> Always(Func<DomainEvent, DomainEvent[]> f) => f;

        
        public static StateDefinition<TState, TInput, TOutput> State<TState, TInput, TOutput>(TState state,
            Func<TInput, TOutput[]>[] onEntry= null, Func<TInput, TOutput[]>[] onState = null,
            Func<TInput, TOutput[]>[] onExit = null)
        {
            onEntry ??= new Func<TInput, TOutput[]>[0];
            onExit ??= new Func<TInput, TOutput[]>[0];
            onState ??= new Func<TInput, TOutput[]>[0];
            return new StateDefinition<TState, TInput, TOutput>(state, onEntry,onExit,onState);
        }
        
        public static StateDefinition<TState, TInput, TOutput> SubState<TState, TInput, TOutput>(TState state, TState parentState,
            Func<TInput, TOutput[]>[] onEntry = null, 
            Func<TInput, TOutput[]>[] onState = null,
            Func<TInput, TOutput[]>[] onExit = null
        )
        {
            onEntry ??= new Func<TInput, TOutput[]>[0];
            onExit ??= new Func<TInput, TOutput[]>[0];
            onState ??= new Func<TInput, TOutput[]>[0];
            return new StateDefinition<TState, TInput, TOutput>(state, parentState, false, onEntry,onExit,onState);
        }
        
        public static StateDefinition<TState, TInput, TOutput> InitialSubState<TState, TInput, TOutput>(TState state, TState parentState,
            Func<TInput, TOutput[]>[] onEntry = null, 
            Func<TInput, TOutput[]>[] onState = null,
            Func<TInput, TOutput[]>[] onExit = null
        )
        {
            onEntry ??= new Func<TInput, TOutput[]>[0];
            onExit ??= new Func<TInput, TOutput[]>[0];
            onState ??= new Func<TInput, TOutput[]>[0];
            return new StateDefinition<TState, TInput, TOutput>(state, parentState, true, onEntry,onExit,onState);
        }
  
        
        public static FunctionDefinition<TInput, TOutput> Function<TInput, TOutput>(Func<TInput, TOutput[]> f, Func<TInput, bool> when=null)
        {
            return new FunctionDefinition<TInput, TOutput>(f, when);
        }

        public static FunctionDefinition<DomainEvent, DomainEvent> Fn(Func<DomainEvent, DomainEvent[]> f, Func<DomainEvent, bool> when=null) =>
            Function(f, when);
        private static Func<TInput, TOutput[]>[] BuildFuncs<TInput, TOutput>(this FunctionDefinition<TInput, TOutput>[] @this) => 
            @this != null ?
            @this.Select(def => new Func<TInput, TOutput[]>(def.Execute)).ToArray()
            : new Func<TInput, TOutput[]>[0];
    }
}