using System.Collections.Generic;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control.Tests.TestUtilities
{
    public static class ImpliciXSystemExtensions
    {
        public static DomainEvent[] PlayEvents(this IImpliciXSystem @this, params DomainEvent[] events)
        {
            var pendingEvents = new Queue<DomainEvent>(events);
            var history = new List<DomainEvent>();
            while (pendingEvents.Count > 0)
            {
                var @event = pendingEvents.Dequeue();
                var resultingEvents = @this.HandleDomainEvent(@event);
                history.AddRange(resultingEvents);
                foreach (var resulting in resultingEvents)
                    pendingEvents.Enqueue(resulting);
            }
            return history.ToArray();
        }
    }
}