using System;
using System.Reactive.Concurrency;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Clock
{
    public class RealClock : IClock
    {
        private readonly IScheduler _scheduler;

        private RealClock(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public static RealClock Create(IScheduler scheduler = null) => new RealClock(scheduler??Scheduler.Default);
        

        public TimeSpan Now() => new TimeSpan(_scheduler.Now.UtcDateTime.Ticks);

        public DateTime DateTimeNow() => _scheduler.Now.UtcDateTime;
       
        public IDisposable Schedule(TimeSpan at, Action action) => _scheduler.Schedule(at, action);
        public IDisposable Schedule(DateTimeOffset time, Action action) => _scheduler.Schedule(time, action);

        public IDisposable SchedulePeriodic(TimeSpan period, Action action) => _scheduler.SchedulePeriodic(period, action);

        public void Advance(TimeSpan timeSpan)
        {
            Log.Error("Real time cannot be 'advanced'");
        }
    }
}