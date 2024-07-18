using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.FrozenTimeSeries.Tests
{
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
            heating = MetricUrn.Build("fake_analytics", nameof(heating));
        }

        public static MetricUrn heating { get; set; }

        public static MetricUrn public_state_A { get; }
        public static MetricUrn public_state_A2 { get; }
        public static MetricUrn public_state_A3 { get; }

        public static MetricUrn sample_metric { get; }
        public static MetricUrn public_state_B { get; }
        public static MetricUrn temperature { get; }
        public static MetricUrn temperature_delta { get; }

        public static Urn daily_timer { get; }
        public static Urn hourly_timer { get; }
        public static Urn other_timer { get; }
    }

    public static class AllMetricsTest
    {
        private static readonly Metric<MetricUrn> PublicStateMetric1 =
            new (MetricKind.State,
                fake_analytics.public_state_A,
                fake_analytics.public_state_A,
                fake_model.public_state,
                TimeSpan.FromDays(1),
                subMetricDefs: new[] {new SubMetricDef("fake_index", MetricKind.Variation, fake_model.fake_index)}
            );

        private static readonly Metric<MetricUrn> PublicStateMetric2 =
            new (MetricKind.State,
                fake_analytics.public_state_B,
                fake_analytics.public_state_B,
                fake_model.public_state2,
                TimeSpan.FromDays(1));

        private static readonly Metric<MetricUrn> GaugeMetric1 =
            new (MetricKind.Gauge,
                fake_analytics.temperature,
                fake_analytics.temperature,
                fake_model.temperature.measure,
                TimeSpan.FromDays(1));

        private static readonly Metric<MetricUrn> VariationMetric1 =
            new (MetricKind.Variation,
                fake_analytics.temperature_delta,
                fake_analytics.temperature_delta,
                fake_model.temperature.measure,
                TimeSpan.FromDays(1));

        public static IMetric[] AllMetrics()
        {
            return new IMetric[] {PublicStateMetric1, PublicStateMetric2, GaugeMetric1, VariationMetric1};
        }
    }
}