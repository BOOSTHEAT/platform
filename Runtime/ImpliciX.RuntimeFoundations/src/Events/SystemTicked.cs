using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Events
{
    public class SystemTicked : PublicDomainEvent, IEquatable<SystemTicked>
    {
        private readonly TimeSpan _origin;

        public static SystemTicked Create(ushort basePeriodMs, uint tickCount) =>
            Create(TimeSpan.Zero, basePeriodMs, tickCount);

        public static SystemTicked Create(TimeSpan origin, ushort basePeriodMs, uint tickCount) =>
            new SystemTicked(origin, basePeriodMs, 
                new TimeSpan(origin.Ticks + TimeSpan.TicksPerMillisecond*tickCount*basePeriodMs),
                tickCount);

        public int BasePeriod { get; }

        public uint TickCount { get; }

        private SystemTicked(TimeSpan origin, ushort baseFrequencyMs, TimeSpan at, uint tickCount) : base(
            Guid.NewGuid(), at)
        {
            _origin = origin;
            BasePeriod = baseFrequencyMs;
            TickCount = tickCount;
        }

        public bool Equals(SystemTicked other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BasePeriod == other.BasePeriod && TickCount == other.TickCount && At == other.At;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SystemTicked)obj);
        }

        public override int GetHashCode() => HashCode.Combine(BasePeriod, TickCount, At);

        public static bool operator ==(SystemTicked left, SystemTicked right) => Equals(left, right);

        public static bool operator !=(SystemTicked left, SystemTicked right) => !Equals(left, right);

        public bool IsPeriodElapsed(TimeSpan period) => (TickCount * BasePeriod) % period.TotalMilliseconds == 0;

        public bool IsMinuteElapsed() => ((TickCount * BasePeriod) / 1000) % 60 == 0;

        public bool IsHourElapsed() => ((TickCount * BasePeriod) / 1000) % 3600 == 0;

        public bool IsNextDate(TimeSpan period) =>
            (Normalize(_origin.TotalMilliseconds) + BasePeriod * TickCount) % Normalize(period.TotalMilliseconds) == 0;

        private long Normalize(double value) => (long)(value / BasePeriod) * BasePeriod;
    }
}