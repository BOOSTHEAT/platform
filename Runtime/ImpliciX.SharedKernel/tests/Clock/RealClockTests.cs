using System;
using ImpliciX.SharedKernel.Clock;
using Microsoft.Reactive.Testing;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests.Clock
{
    [TestFixture]
    public class RealClockTests
    {
        [Test]
        public void schedule_action_to_be_executed_one_time_at_a_given_time()
        {
            var scheduler = new TestScheduler();
            var sut = RealClock.Create(scheduler);
            var i = 0;
            sut.Schedule(TimeSpan.FromMilliseconds(1000), () => i++);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
            Check.That(i).IsEqualTo(0);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000).Ticks);
            Check.That(i).IsEqualTo(1);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000).Ticks);
            Check.That(i).IsEqualTo(1);
        }

        [Test]
        public void schedule_periodic_action()
        {
            var scheduler = new TestScheduler();
            var sut = RealClock.Create(scheduler);
            var i = 0;
            sut.SchedulePeriodic(TimeSpan.FromMilliseconds(1000), () => i++);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(100).Ticks);
            Check.That(i).IsEqualTo(0);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000).Ticks);
            Check.That(i).IsEqualTo(1);
            scheduler.AdvanceBy(TimeSpan.FromMilliseconds(1000).Ticks);
            Check.That(i).IsEqualTo(2);
        }
    }
}