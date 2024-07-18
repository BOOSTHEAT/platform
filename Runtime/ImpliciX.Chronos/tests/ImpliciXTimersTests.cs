using System;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.PropertiesChangedHelper;

namespace ImpliciX.Chronos.Tests
{
    public class ImpliciXTimersTests
    {
        [Test]
        public void many_timers_test()
        {
            var (sut, clock, spyEventBus) = CreateSut();

            sut.HandleTimeoutRequest(NotifyOnTimeoutRequested_10sec_Timer);
            clock.Advance(TimeSpan.FromMilliseconds(_10sec_Timeout.Milliseconds));
            sut.HandleTimeoutRequest(NotifyOnTimeoutRequested_20secTimer);
            clock.Advance(TimeSpan.FromMilliseconds(_20sec_Timout.Milliseconds));

            var expectedEvens = new DomainEvent[]
            {
                TimeoutOccured.Create(Timers.First, _t0.Add(TimeSpan.FromMilliseconds(_10sec_Timeout.Milliseconds)),
                    NotifyOnTimeoutRequested_10sec_Timer.EventId),
                TimeoutOccured.Create(Timers.Second, _t0
                    .Add(TimeSpan.FromMilliseconds(_10sec_Timeout.Milliseconds))
                    .Add(TimeSpan.FromMilliseconds(_20sec_Timout.Milliseconds)), NotifyOnTimeoutRequested_20secTimer.EventId),
            };
            Check.That(spyEventBus.RecordedPublications).ContainsExactly(expectedEvens);
        }


        [Test]
        public void start_should_cancel_a_previous_timer_with_a_same_urn_and_start_a_new_one()
        {
            var (sut, clock, spyEventBus) = CreateSut();

            sut.HandleTimeoutRequest(NotifyOnTimeoutRequested_10sec_Timer);
            clock.Advance(_5sec);
            Check.That(spyEventBus.RecordedPublications).IsEmpty();

            sut.HandleTimeoutRequest(NotifyOnTimeoutRequested_10sec_Timer);
            clock.Advance(_5sec);
            Check.That(spyEventBus.RecordedPublications).IsEmpty();

            clock.Advance(_5sec);
            Check.That(spyEventBus.RecordedPublications)
                .ContainsExactly(TimeoutOccured.Create(Timers.First, _t0.Add(_5sec * 3), NotifyOnTimeoutRequested_10sec_Timer.EventId));
        }

        private static (ImpliciXTimers sut, VirtualClock clock, SpyEventBus spyEventBus) CreateSut()
        {
            var spyEventBus = new SpyEventBus();
            var clock = VirtualClock.Create();
            var sut = new ImpliciXTimers((to) => spyEventBus.Publish(to), clock);
            var persistentChanged = CreatePropertyChanged(_t0, (Timers.First, _10sec_Timeout), (Timers.Second, _20sec_Timout));
            sut.HandlePersistentChanged(persistentChanged);
            return (sut, clock, spyEventBus);
        }


        private static readonly Duration _10sec_Timeout = Duration.FromFloat(10000).GetValueOrDefault();
        private static readonly TimeSpan _5sec = TimeSpan.FromMilliseconds((uint) (_10sec_Timeout.Milliseconds * 0.5));
        private static readonly Duration _20sec_Timout = Duration.FromFloat(20000).GetValueOrDefault();
        private static readonly TimeSpan _t0 = TimeSpan.Zero;
        private static readonly TimeSpan _10sec = TimeSpan.FromMilliseconds((uint) (_10sec_Timeout.Milliseconds));
        private static readonly NotifyOnTimeoutRequested NotifyOnTimeoutRequested_10sec_Timer = NotifyOnTimeoutRequested.Create(Timers.First, _t0);
        private static readonly NotifyOnTimeoutRequested NotifyOnTimeoutRequested_20secTimer = NotifyOnTimeoutRequested.Create(Timers.Second, _t0);
        // private static readonly NotifyOnTimeRequested NotifyOnTimeRequested_Timer = NotifyOnTimeRequested.Create(timers.first, DateTimeOffset.Now.Add(new TimeSpan(0,0,10)), _t0); 

        private class Timers
        {
            public static Urn First => Urn.BuildUrn(nameof(Timers), nameof(First));
            public static Urn Second => Urn.BuildUrn(nameof(Timers), nameof(Second));
        }
    }
}