using System;
using System.Reactive.Concurrency;

namespace ImpliciX.SharedKernel.Clock
{
    public class VirtualClock : IClock
    {
        private readonly VirtualTimeScheduler _scheduler;

        public static VirtualClock Create(DateTime? startTime = null)
        {
            return new VirtualClock(startTime ?? new DateTime());
        }

        public VirtualClock(DateTime startTime)
        {
            _scheduler = new VirtualTimeScheduler();
            _scheduler.AdvanceTo(startTime.Ticks);
        }

        public TimeSpan Now() => new TimeSpan(_scheduler.Now.UtcDateTime.Ticks);

        public void Advance(TimeSpan timeSpan) => _scheduler.AdvanceBy(timeSpan.Ticks);

        public DateTime DateTimeNow() => new DateTime(_scheduler.Now.UtcDateTime.Ticks, DateTimeKind.Utc);

        public IDisposable Schedule(TimeSpan at, Action action) => _scheduler.Schedule(at, action);
        public IDisposable Schedule(DateTimeOffset time, Action action) => _scheduler.Schedule(time, action);

        public IDisposable SchedulePeriodic(TimeSpan period, Action action) => _scheduler.SchedulePeriodic(period, action);


        private class VirtualTimeScheduler : VirtualTimeScheduler<long, long>
        {
            protected override long Add(long absolute, long relative) => absolute + relative;
            protected override DateTimeOffset ToDateTimeOffset(long absolute) => new DateTimeOffset(absolute, TimeSpan.Zero);
            protected override long ToRelative(TimeSpan timeSpan) => timeSpan.Ticks;
        }
    }
}