using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;

namespace ImpliciX.Data.Tests.Metrics;

internal static class MetricsHelpers
{
  public static Urn ToUrn(string urnStr) => MFactory.ToUrn(urnStr);
  public static MetricUrn ToMUrn(string urnStr) => MFactory.ToMUrn(urnStr);

  public static Metric<MetricUrn> CreateMetricGauge(string outputUrn, Urn? inputUrn = null, int? period = null)
    => MFactory.CreateGaugeMetric(MetricUrn.Build(outputUrn), inputUrn ?? "foo:inputUrn", period ?? 5)
      .Builder.Build<Metric<MetricUrn>>();

  public static Metric<MetricUrn> CreateMetricAccumulator(string rootUrn, Urn? inputUrn = null, int? period = null)
    => MFactory.CreateAccumulatorMetric(MetricUrn.Build(rootUrn), inputUrn ?? "foo:inputUrn", period ?? 5)
      .Builder.Build<Metric<MetricUrn>>();

  public static Metric<MetricUrn> CreateMetricVariation(string outputUrn, Urn? inputUrn = null, int? period = null)
    => MFactory.CreateVariationMetric(MetricUrn.Build(outputUrn), inputUrn ?? "foo:inputUrn", period ?? 5)
      .Builder.Build<Metric<MetricUrn>>();

  public static Metric<MetricUrn> CreateMetricStateMonitoring<TState>(string outputUrn, PropertyUrn<TState>? inputUrn, int? period = null)
    => MFactory.CreateStateMonitoringOfMetric(MetricUrn.Build(outputUrn), inputUrn, period ?? 5)
      .Builder.Build<Metric<MetricUrn>>();
}

public enum PubState
{
  Disabled,
  Active
}