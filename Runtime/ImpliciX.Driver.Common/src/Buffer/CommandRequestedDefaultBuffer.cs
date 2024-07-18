using System.Collections.Generic;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Driver.Common.Buffer
{
    public class CommandRequestedDefaultBuffer : ICommandRequestedBuffer
    {
        private readonly List<CommandRequested> _receivedEvents;

        public CommandRequestedDefaultBuffer()
        {
            _receivedEvents = new List<CommandRequested>();
        }

        public void ReceivedCommandRequested(CommandRequested commandRequested)
        {
            _receivedEvents.Add(commandRequested);
        }

        public IEnumerable<CommandRequested> ReleaseCommandRequested()
        {
            var releaseDomainEvents = (_receivedEvents).ToArray();

            _receivedEvents.Clear();

            return releaseDomainEvents;
        }
    }
}