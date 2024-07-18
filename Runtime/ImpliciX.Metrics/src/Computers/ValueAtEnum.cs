using System;

namespace ImpliciX.Metrics.Computers
{
    internal readonly struct ValueAtEnum
    {
        public float Value { get; }
        public TimeSpan At { get; }
        public Enum EnumValue { get; }

        public ValueAtEnum(float value, TimeSpan at, Enum enumValue)
        {
            Value = value;
            At = at;
            EnumValue = enumValue;
        }
    }
}