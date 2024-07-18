using System;

namespace ImpliciX.SharedKernel.Clock
{
    public interface IClock
    {
        TimeSpan Now();

        void Advance(TimeSpan timeSpan);

        DateTime DateTimeNow();

        IDisposable Schedule(TimeSpan at, Action action);

        IDisposable Schedule(DateTimeOffset time, Action action);
        IDisposable SchedulePeriodic(TimeSpan period, Action action);
    }
}