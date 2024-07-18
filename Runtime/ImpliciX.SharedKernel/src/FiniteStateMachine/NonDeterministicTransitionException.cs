using System;
using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.SharedKernel.FiniteStateMachine
{
    [Serializable]
    public class NonDeterministicTransitionException<TState> : Exception
    {
        public NonDeterministicTransitionException(TState fromState, IEnumerable<TState> candidates) : base(FormatMessage(fromState, candidates))
        {
        }

        private static string FormatMessage(TState fromState, IEnumerable<TState> candidates)
        {
            return $"From state {fromState} many transitions are possible {CandidatesAsString(candidates)}";
        }

        private static string CandidatesAsString(IEnumerable<TState> candidates)
        {
            return candidates.Aggregate("", (m, n) => $"{m.ToString()},{n.ToString()}");
        }
    }
}