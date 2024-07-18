using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.Driver.Common.Buffer
{
    public static class BufferedController
    {
        public static DomainEventHandler<DomainEvent> BufferedHandler(DomainEventHandler<DomainEvent> handler, ICommandRequestedBuffer buffer, IClock clock) =>
            @event => @event switch
            {
                CommandRequested commandRequested => HandleCommandRequested(commandRequested, buffer),
                SystemTicked systemTicked => HandleSystemTicked(systemTicked, handler, buffer, clock),
                _ => handler(@event)
            };

        private static DomainEvent[] HandleCommandRequested(CommandRequested commandRequested, ICommandRequestedBuffer buffer)
        {
            buffer.ReceivedCommandRequested(commandRequested);
            return Array.Empty<DomainEvent>();
        }

        private static DomainEvent[] HandleSystemTicked(SystemTicked systemTicked, DomainEventHandler<DomainEvent> handler, ICommandRequestedBuffer buffer,
            IClock clock)
        {
            if (IsObsolete(systemTicked, clock.Now())) return Array.Empty<DomainEvent>();
            return buffer.ReleaseCommandRequested()
                .Concat(systemTicked.AsEnumerable())
                .SelectMany(c => handler(c))
                .ToArray();
        }

        private static bool IsObsolete(SystemTicked systemTicked, TimeSpan currentTime) =>
            systemTicked.At + TimeSpan.FromMilliseconds(systemTicked.BasePeriod) < currentTime;

        private static IEnumerable<DomainEvent> AsEnumerable(this SystemTicked systemTicked)
        {
            yield return systemTicked;
        }
    }
}