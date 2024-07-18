using System;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.TestsCommon
{
    public class StubClock : IClock
    {
        public TimeSpan Now() => TimeSpan.Zero;

        public void Advance(TimeSpan timeSpan) => throw new NotImplementedException();

        public DateTime DateTimeNow() => throw new NotImplementedException();
        public IDisposable Schedule(TimeSpan at, Action action)
        {
            throw new NotImplementedException();
        }

        public IDisposable Schedule(DateTimeOffset time, Action action)
        {
            throw new NotImplementedException();
        }

        public IDisposable SchedulePeriodic(TimeSpan period, Action action)
        {
            throw new NotImplementedException();
        }
    }
}