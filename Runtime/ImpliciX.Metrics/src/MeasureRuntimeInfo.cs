using System;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;

namespace ImpliciX.Metrics
{
    internal readonly struct MeasureRuntimeInfo
    {
        public StateMonitoringMeasureKind MeasureKind { get; }
        public string MeasureName { get; }
        public Urn InputUrn { get; }

        public MeasureRuntimeInfo(StateMonitoringMeasureKind metricKind, string measureName, Urn inputUrn)
        {
            MeasureKind = metricKind;
            MeasureName = measureName ?? throw new ArgumentNullException(nameof(measureName));
            InputUrn = inputUrn ?? throw new ArgumentNullException(nameof(inputUrn));
        }

        public MeasureRuntimeInfo(SubMetricDef def)
        {
            MeasureName = def.SubMetricName;
            MeasureKind = def.MetricKind.ToSateMonitoringMeasureKind();
            InputUrn = def.InputUrn;
        }
    }
}