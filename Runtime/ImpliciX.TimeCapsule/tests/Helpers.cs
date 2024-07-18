using ImpliciX.Language.Model;

namespace ImpliciX.TimeCapsule.Tests;

public class Helpers
{
    public static MetricValue MV(float v)
    {
        return new MetricValue(v, TimeSpan.Zero, TimeSpan.Zero);
    }
}