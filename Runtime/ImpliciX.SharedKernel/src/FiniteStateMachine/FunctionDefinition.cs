using System;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    public class FunctionDefinition<TInput, TOutput>
    {
        private readonly Func<TInput, TOutput[]> _function;
        private readonly Func<TInput, bool> _guard;

        public FunctionDefinition(Func<TInput, TOutput[]> function, Func<TInput, bool> guard=null)
        {
            _function = function;
            _guard = guard ?? (_ => true);
        }

        public TOutput[] Execute(TInput input) => 
            _guard(input) ? 
                _function(input) 
                : new TOutput[0];
    }
}