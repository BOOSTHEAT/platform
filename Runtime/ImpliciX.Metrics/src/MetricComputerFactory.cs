using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics
{
    internal sealed class MetricComputerFactory
    {
        private readonly IReadTimeSeries _tsReader;
        private readonly IWriteTimeSeries _tsWriter;

        public MetricComputerFactory(IReadTimeSeries tsReader, IWriteTimeSeries tsWriter)
        {
            _tsReader = tsReader;
            _tsWriter = tsWriter;
        }

        public ComputerRuntimeInfo[] Create(Metric<MetricUrn> metric, TimeSpan now)
        {
            if (metric == null) throw new ArgumentNullException(nameof(metric));

            if (metric.Kind == MetricKind.State) return CreateStateMonitoring(metric, now);

            var outputUrn = metric.Target;
            var metricKey = ((IMetric) metric).Key;
            var windowPeriod = GetWindowPeriod(metric.WindowPolicy);

            var computerInfosForGroupPolicies = metric.GroupPolicies
                .Select(group =>
                    (
                        group.Period,
                        TargetUrn: MetricUrn.Build(outputUrn, group.Name)
                    ))
                .Select(pair =>
                    (
                        pair.Period,
                        Computer: CreateComputer(metric.Kind, pair.TargetUrn, pair.Period, windowPeriod, now)
                    ))
                .Select(pair => new ComputerRuntimeInfo(metricKey, metric.InputUrns(), pair.Period, AssumeComputerIsSome(pair.Computer)));

            var computerInfoForMetricPublicationPeriod = new ComputerRuntimeInfo(metricKey, metric.InputUrns(), metric.PublicationPeriod,
                AssumeComputerIsSome(CreateComputer(metric.Kind, outputUrn, metric.PublicationPeriod, windowPeriod, now)));

            return computerInfosForGroupPolicies
                .Append(computerInfoForMetricPublicationPeriod)
                .ToArray();
        }

        private ComputerRuntimeInfo[] CreateStateMonitoring(Metric<MetricUrn> metric, TimeSpan now)
        {
            var validSubMetricDefs = AssumeAllSubMetricDefsAreValid(metric.SubMetricDefs.ToArray());
            var stateMonitoringComputerRuntimeInfos = new List<ComputerRuntimeInfo>();

            var infosForIncludedMetrics = validSubMetricDefs
                .Select(def => new MeasureRuntimeInfo(def))
                .ToArray();

            var outputUrn = metric.Target;
            var metricKey = ((IMetric) metric).Key;

            // Main period
            var windowPeriod = GetWindowPeriod(metric.WindowPolicy);
            var stateType = GetStateType(metric.InputType.GetValue(), metric.InputUrn);
            if (stateType is null)
                throw new InvalidOperationException($"Cannot get state type to build {nameof(StateMonitoringComputer)} for urn {metric.InputUrn}");

            stateMonitoringComputerRuntimeInfos.Add(
                new ComputerRuntimeInfo(metricKey, metric.InputUrns(), metric.PublicationPeriod, new StateMonitoringComputer(outputUrn,
                    stateType, metric.PublicationPeriod, windowPeriod, infosForIncludedMetrics, _tsReader, _tsWriter, now)
                )
            );

            // Periods from Groups
            metric.GroupPolicies.ForEach(group =>
            {
                stateMonitoringComputerRuntimeInfos.Add(
                    new ComputerRuntimeInfo(metricKey, metric.InputUrns(), group.Period, new StateMonitoringComputer(MetricUrn.Build(outputUrn, group.Name),
                        stateType, group.Period, windowPeriod, infosForIncludedMetrics, _tsReader, _tsWriter, now)
                    )
                );
            });

            return stateMonitoringComputerRuntimeInfos.ToArray();
        }

        private static Type? GetStateType(Type inputType, Urn metricInputUrn)
            => inputType == typeof(SubsystemState)
                ? GetStateTypeFromSubSystemDefinition(metricInputUrn)
                : inputType;

        private static Type? GetStateTypeFromSubSystemDefinition(Urn metricInputUrn)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var definedTypes = assemblies.SelectMany(o => o.DefinedTypes);
            var instances = definedTypes
                .Where(t => t.BaseType is {IsGenericType: true}
                            && t.BaseType.GetGenericTypeDefinition() == typeof(SubSystemDefinition<>)
                            && t.IsGenericType is false)
                .Select(def => (ISubSystemDefinition) Activator.CreateInstance(def));

            return instances
                .Where(o => o.StateUrn == metricInputUrn)
                .Select(o => o.StateType)
                .FirstOrDefault();
        }

        private static TimeSpan? GetWindowPeriod(Option<WindowPolicy> windowPolicy)
        {
            if (windowPolicy.IsNone) return null;
            return windowPolicy.GetValue().ToTimeSpan();
        }

        private Option<IMetricComputer> CreateComputer(MetricKind metricKind, MetricUrn outputUrn, TimeSpan publicationPeriod,
            TimeSpan? windowPeriod, TimeSpan now)
        {
            return metricKind switch
            {
                MetricKind.Gauge => new GaugeComputer(outputUrn, _tsReader, _tsWriter, now),
                MetricKind.Variation => new VariationComputer(outputUrn, publicationPeriod, windowPeriod, _tsReader, _tsWriter, now),
                MetricKind.SampleAccumulator => new AccumulatorComputer(outputUrn, publicationPeriod, windowPeriod, _tsReader, _tsWriter, now),
                _ => Option<IMetricComputer>.None()
            };
        }

        private static IMetricComputer AssumeComputerIsSome(Option<IMetricComputer> computer)
            => computer.IsNone
                ? throw new InvalidOperationException($"{nameof(computer)} must not be None")
                : computer.GetValue();

        private static SubMetricDef[] AssumeAllSubMetricDefsAreValid(IReadOnlyCollection<SubMetricDef> metricSubMetricDefs)
        {
            metricSubMetricDefs.ForEach(def => def.MetricKind.ToSateMonitoringMeasureKind());
            return metricSubMetricDefs.ToArray();
        }
    }

    internal struct ComputerRuntimeInfo
    {
        public IEnumerable<Urn> TriggerUrns { get; }
        public string MetricKey { get; }
        public TimeSpan PublicationPeriod { get; }
        public IMetricComputer Computer { get; }

        public ComputerRuntimeInfo(string metricKey, IEnumerable<Urn> triggerUrns, TimeSpan publicationPeriod, IMetricComputer computer)
        {
            MetricKey = metricKey ?? throw new ArgumentNullException(nameof(metricKey));
            TriggerUrns = triggerUrns ?? throw new ArgumentNullException(nameof(triggerUrns));
            PublicationPeriod = publicationPeriod;
            Computer = computer ?? throw new ArgumentNullException(nameof(computer));
        }
    }
}