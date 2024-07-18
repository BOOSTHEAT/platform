using System;

namespace ImpliciX.Data.Api;

public class TimeSeriesValue
{
    public TimeSeriesValue(DateTime at, float value)
    {
        At = at;
        Value = value;
    }

    public DateTime At { get; }
    public float Value { get; }
}