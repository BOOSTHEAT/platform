using System;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    public class Transition<TState,TInput>
    {
        public TState From { get; }
        public TState To { get; }
        public Func<TInput,bool> Condition {get;}
        public Transition(TState @from, TState to, Func<TInput,bool> condition = null)
        {
            condition ??= (_) => true;
            Condition = condition;
            From = @from;
            To = to;
        }
    }
}