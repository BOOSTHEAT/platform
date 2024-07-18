using ImpliciX.Language.Model;

namespace ImpliciX.HttpTimeSeries.Tests.Helpers;

public class fake_analytics : RootModelNode
{
  public fake_analytics() : base("fake_analytics")
  {
  }

  static fake_analytics()
  {
    public_state_A = MetricUrn.Build("fake_analytics", nameof(public_state_A));
    public_state_B = MetricUrn.Build("fake_analytics", nameof(public_state_B));
    public_state_A2 = MetricUrn.Build("fake_analytics", nameof(public_state_A2));
    public_state_A3 = MetricUrn.Build("fake_analytics", nameof(public_state_A3));
    temperature = MetricUrn.Build("fake_analytics", nameof(temperature));
    temperature_delta = MetricUrn.Build("fake_analytics", nameof(temperature_delta));

    daily_timer = Urn.BuildUrn("fake_analytics", nameof(daily_timer));
    hourly_timer = Urn.BuildUrn("fake_analytics", nameof(hourly_timer));
    other_timer = Urn.BuildUrn("fake_analytics", nameof(other_timer));

    sample_metric = MetricUrn.Build("fake_analytics", nameof(sample_metric));
    accumulated = MetricUrn.Build("fake_analytics", nameof(accumulated));
    heating = MetricUrn.Build("fake_analytics", nameof(heating));
  }

  public static MetricUrn heating { get; set; }

  public static MetricUrn public_state_A { get; }
  public static MetricUrn public_state_A2 { get; }
  public static MetricUrn public_state_A3 { get; }

  public static MetricUrn sample_metric { get; }
  public static MetricUrn accumulated { get; }
  public static MetricUrn public_state_B { get; }
  public static MetricUrn temperature { get; }
  public static MetricUrn temperature_delta { get; }

  public static Urn daily_timer { get; }
  public static Urn hourly_timer { get; }
  public static Urn other_timer { get; }
}