using System;
using ImpliciX.Language.Metrics;
using ImpliciX.ReferenceApp.Model;
using static ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.ReferenceApp.App;

internal static class AllTimeMath
{
  public static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(5);

  public static readonly IMetricDefinition[] Declarations =
  {
    Metric(system.timemath.heat.dhw_power).Is.Every(5).Seconds.GaugeOf(monitoring.dhw.power.measure),
    Metric(system.timemath.heat.heating_power).Is.Every(5).Seconds.GaugeOf(monitoring.heating.power.measure)
  };
}