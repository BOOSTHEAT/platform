using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.Language.Core;

namespace ImpliciX.Driver.Common.Buffer
{
    public class CommandRequestedBuffer : ICommandRequestedBuffer
    {
        public CommandRequestedBuffer(Dictionary<Type, ICommandRequestedBuffer> aggregatorMap)
        {
            _defaultCollector = new CommandRequestedDefaultBuffer();
            _aggregatorMap = aggregatorMap;
        }

        public void ReceivedCommandRequested(CommandRequested commandRequested)
        {
            _aggregatorMap.Get(commandRequested.Arg.GetType())
                .Tap(
                    error => { _defaultCollector.ReceivedCommandRequested(commandRequested); },
                    aggregator => aggregator.ReceivedCommandRequested(commandRequested)
                );
        }

        public IEnumerable<CommandRequested> ReleaseCommandRequested() =>
            _aggregatorMap.Aggregate(
                _defaultCollector.ReleaseCommandRequested(),
                (cr, pair) => cr.Concat(pair.Value.ReleaseCommandRequested())
            ).OrderBy(cr => cr.At);

        private readonly ICommandRequestedBuffer _defaultCollector;

        private readonly Dictionary<Type, ICommandRequestedBuffer> _aggregatorMap;
    }
}