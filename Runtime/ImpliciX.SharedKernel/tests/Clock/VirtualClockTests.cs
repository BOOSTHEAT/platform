using System;
using ImpliciX.SharedKernel.Clock;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests.Clock
{
    [TestFixture]
    public class VirtualClockTests
    {
        [Test]
        public void should_give_the_current_time_relative_to_default_start_time()
        {
            var sut = VirtualClock.Create();
            var t = sut.DateTimeNow();
            Check.That(t).IsEqualTo(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void should_advance_time()
        {
            var startTime = new DateTime(1998, 7, 12, 13, 15, 0, DateTimeKind.Utc);
            var sut = VirtualClock.Create(startTime);
            var t = sut.DateTimeNow();
            Check.That(t).IsEqualTo(startTime);
            sut.Advance(TimeSpan.FromDays(1));
            Check.That(sut.DateTimeNow()).IsEqualTo(startTime.AddDays(1));
        }

        [Test]
        public void schedule_action_to_be_executed_one_time_at_a_given_time()
        {
            var sut = VirtualClock.Create();
            var i = 0;
            sut.Schedule(TimeSpan.FromMilliseconds(1000), () => i++);
            sut.Advance(TimeSpan.FromMilliseconds(100));
            Check.That(i).IsEqualTo(0);
            sut.Advance(TimeSpan.FromMilliseconds(1000));
            Check.That(i).IsEqualTo(1);
            sut.Advance(TimeSpan.FromMilliseconds(1000));
            Check.That(i).IsEqualTo(1);
        }

        [Test]
        public void schedule_periodic_action()
        {
            var sut = VirtualClock.Create();
            var i = 0;
            sut.SchedulePeriodic(TimeSpan.FromMilliseconds(1000), () => i++);
            sut.Advance(TimeSpan.FromMilliseconds(100));
            Check.That(i).IsEqualTo(0);
            sut.Advance(TimeSpan.FromMilliseconds(1000));
            Check.That(i).IsEqualTo(1);
            sut.Advance(TimeSpan.FromMilliseconds(1000));
            Check.That(i).IsEqualTo(2);
        }
    }
}