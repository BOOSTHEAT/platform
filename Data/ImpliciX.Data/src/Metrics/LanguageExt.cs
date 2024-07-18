using System;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Metrics;

public static class WindowedPolicyExtensions
{
    public static TimeSpan ToTimeSpan(this WindowPolicy windowPolicy)
        => TimeUnitMultiplierToTimeSpan(windowPolicy.TimeUnit, windowPolicy.TimeUnitMultiplier);

    public static TimeSpan ToTimeSpan(this StoragePolicy storagePolicy)
        => TimeUnitMultiplierToTimeSpan(storagePolicy.TimeUnit, storagePolicy.Duration);
    
    private static TimeSpan TimeUnitMultiplierToTimeSpan(TimeUnit timeUnit, int multiplier)
        => timeUnit switch
        {
            TimeUnit.Seconds => TimeSpan.FromSeconds(1 * multiplier),
            TimeUnit.Minutes => TimeSpan.FromMinutes(1 * multiplier),
            TimeUnit.Hours => TimeSpan.FromHours(1 * multiplier),
            TimeUnit.Days => TimeSpan.FromDays(1 * multiplier),
            TimeUnit.Weeks => TimeSpan.FromDays(7 * multiplier),
            TimeUnit.Months => TimeSpan.FromDays(365f / 12f * multiplier),
            TimeUnit.Quarters => TimeSpan.FromDays(365f / 4f * multiplier),
            TimeUnit.Years => TimeSpan.FromDays(365 * multiplier),
            _ => throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null)
        };
}