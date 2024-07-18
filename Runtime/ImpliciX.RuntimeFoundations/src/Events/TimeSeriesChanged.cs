using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;
namespace ImpliciX.RuntimeFoundations.Events
{
    public record TimeSeriesValue(TimeSpan At, float Value)
    {
        public float Value { get; } = Value;
        public TimeSpan At { get; } = At;
    }


    public class TimeSeriesChanged : PublicDomainEvent
    {
        public Urn Urn { get; }
        public Dictionary<Urn, HashSet<TimeSeriesValue>> TimeSeries { get; }

        public TimeSeriesChanged(TimeSpan at) : base(Guid.NewGuid(), at)
        {
        }

        private TimeSeriesChanged(Urn urn, Dictionary<Urn, HashSet<TimeSeriesValue>> timeSeries, TimeSpan at) : this(at)
        {
            Urn = urn;
            TimeSeries = timeSeries;
        }

        public static TimeSeriesChanged Create(Urn urn, Dictionary<Urn, HashSet<TimeSeriesValue>> timeSeries, TimeSpan at)
        {
            return new TimeSeriesChanged(urn, timeSeries, at);
        }
    }
}