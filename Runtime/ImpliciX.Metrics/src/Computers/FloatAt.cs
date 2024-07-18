using System;

namespace ImpliciX.Metrics.Computers
{
    public readonly struct FloatAt
    {
        public float Value { get; }
        public TimeSpan At { get; }

        public FloatAt(float value, TimeSpan at)
        {
            Value = value;
            At = at;
        }
    }
}