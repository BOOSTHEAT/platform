using System;

namespace ImpliciX.TimeSeries
{
    public static class TimespanExtension
    {
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerTenthSecond = TicksPerMillisecond * 100; 
        
        public static TimeSpan Round(this TimeSpan ts,Precision precision)
        {
            return precision switch
            {
                Precision.Millisecond => new TimeSpan((ts.Ticks / TicksPerMillisecond) * TicksPerMillisecond),
                Precision.TenthOfSecond => new TimeSpan((ts.Ticks / TicksPerTenthSecond) * TicksPerTenthSecond),
                Precision.Second => new TimeSpan((ts.Ticks / TicksPerSecond) * TicksPerSecond),
                _ => throw new ArgumentOutOfRangeException(nameof(precision), precision, null)
            };
        }
        
        public enum Precision
        {
            Millisecond,
            TenthOfSecond,
            Second
        }
    }
}