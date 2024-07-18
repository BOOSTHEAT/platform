using System;

namespace ImpliciX.TimeMath.Access;

public readonly struct FloatValueAt
{
    public float Value { get; }
    public TimeSpan At { get; }

    public FloatValueAt(TimeSpan updateValueAt, float value)
    {
        At = updateValueAt;
        Value = value;
    }
}
