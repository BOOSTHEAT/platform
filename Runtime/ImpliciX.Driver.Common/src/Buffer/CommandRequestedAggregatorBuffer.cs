using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Driver.Common.Buffer
{
    public class CommandRequestedAggregatorBuffer : ICommandRequestedBuffer
    {
        private readonly Func<CommandRequested, CommandRequested, Result<CommandRequested>> _aggregatorFunction;
        private readonly Dictionary<Urn, CommandRequested> _aggregatedEvents;

        public CommandRequestedAggregatorBuffer(
            Func<CommandRequested, CommandRequested, Result<CommandRequested>> aggregatorFunction)
        {
            _aggregatorFunction = aggregatorFunction;
            _aggregatedEvents = new Dictionary<Urn, CommandRequested>();
        }

        public void ReceivedCommandRequested(CommandRequested commandRequested)
        {
            var aggregatedValueResult =
                from previousCommand in _aggregatedEvents.GetOrDefault(commandRequested.Urn, commandRequested)
                from aggregatedValue in _aggregatorFunction(previousCommand, commandRequested)
                select aggregatedValue;

            aggregatedValueResult.Tap(
                aggregated => { _aggregatedEvents[commandRequested.Urn] = aggregated; }
            );
        }

        public IEnumerable<CommandRequested> ReleaseCommandRequested()
        {
            var releaseDomainEvents = _aggregatedEvents.Values.ToArray();

            _aggregatedEvents.Clear();

            return releaseDomainEvents;
        }
    }
}