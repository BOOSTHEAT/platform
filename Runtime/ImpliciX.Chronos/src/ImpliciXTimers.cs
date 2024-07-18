#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using Duration = ImpliciX.Language.Model.Duration;

namespace ImpliciX.Chronos
{
    public delegate void Publish(TimeoutOccured timeoutEvent);
    public class ImpliciXTimers
    {
        private readonly Publish _publish;
        private readonly IClock _clock;
        private readonly ConcurrentDictionary<Urn, Duration?> _timeoutsConfiguration;
        private readonly ConcurrentDictionary<Urn, IDisposable> _currentSchedule;
        public ImpliciXTimers(Publish publish, IClock clock)
        {
            _publish = publish;
            _clock = clock;
            _timeoutsConfiguration = new ConcurrentDictionary<Urn, Duration?>();
            _currentSchedule = new ConcurrentDictionary<Urn, IDisposable>();
        }
        private TimeSpan CurrentTime => _clock.Now();
        public DomainEvent[] HandleTimeoutRequest(NotifyOnTimeoutRequested trigger)
        {
            _currentSchedule.AddOrUpdate(trigger.TimerUrn,
                _ => ScheduleDurationTimer(trigger),
                (_, previousTimer) =>
                {
                    previousTimer.Dispose();
                    return ScheduleDurationTimer(trigger);
                }
            );
            return new DomainEvent[] { };
        }


        private IDisposable ScheduleDurationTimer(NotifyOnTimeoutRequested trigger)
        {
            var timeoutConfig = _timeoutsConfiguration.GetValueOrDefault(trigger.TimerUrn);
            Release.Ensure(() => timeoutConfig.HasValue, () => $"No configuration for timer {trigger.TimerUrn}");
            var ts = TimeSpan.FromMilliseconds(timeoutConfig!.Value.Milliseconds);
            return _clock.Schedule(ts, () =>
            {
                _publish(TimeoutOccured.Create(trigger.TimerUrn, CurrentTime, trigger.EventId));
            });
        }

        public DomainEvent[] HandlePersistentChanged(PropertiesChanged trigger)
        {
            var changedTimeouts = trigger.ModelValues.Where(v => v.ModelValue() is Duration);
            foreach (var timeout in changedTimeouts)
            {
                _timeoutsConfiguration.AddOrUpdate(timeout.Urn, _ => (Duration) timeout.ModelValue(),
                    (_, __) => (Duration) timeout.ModelValue());
            }

            return new DomainEvent[] { };
        }
    }
}