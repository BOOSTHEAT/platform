using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class SpyActions
    {
        private ConcurrentQueue<object> _recordedEvents;

        public SpyActions()
        {
            _recordedEvents = new ConcurrentQueue<object>();
        }

        public IList<object> RecordedEvents => _recordedEvents.ToImmutableList();

        public void Record(object message)
        {
            _recordedEvents.Enqueue(message);
        }

    }
}