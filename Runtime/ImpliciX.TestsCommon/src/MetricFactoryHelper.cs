using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.TestsCommon
{
  public static class MetricFactoryHelper
  {
    public static StandardMetric CreateGaugeMetric(MetricUrn outputUrn, Urn inputUrn, int everyInMinutes)
      => MetricsDSL.Metric(outputUrn)
        .Is
        .Every(everyInMinutes).Minutes
        .GaugeOf(inputUrn);

    public static StandardMetric CreateVariationMetric(MetricUrn outputUrn, Urn inputUrn, int everyInMinutes)
      => MetricsDSL.Metric(outputUrn)
        .Is
        .Every(everyInMinutes).Minutes
        .VariationOf(inputUrn);

    public static StandardMetric CreateAccumulatorMetric(MetricUrn outputUrn, Urn inputUrn, int everyInMinutes)
      => MetricsDSL.Metric(outputUrn)
        .Is
        .Every(everyInMinutes).Minutes
        .AccumulatorOf(inputUrn);

    public static ScheduledStateMetric CreateStateMonitoringOfMetric<T>(MetricUrn outputUrn, PropertyUrn<T> inputUrn, int everyInMinutes)
      => MetricsDSL.Metric(outputUrn)
        .Is
        .Every(everyInMinutes).Minutes
        .StateMonitoringOf(inputUrn);

    public static Urn ToUrn(string urn) => Urn.BuildUrn(urn);
    public static MetricUrn ToMUrn(string urn) => MetricUrn.Build(urn);
  }
}