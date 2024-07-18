using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Metrics.Computers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Collections;

namespace ImpliciX.Metrics
{
    internal sealed class MetricsService
    {
        private readonly Func<TimeSpan> _now;
        private readonly TimeSpan _snapshotInterval;

        private Dictionary<Urn, IMetricComputer[]> _computersTriggerUrn;
        private Dictionary<string, IMetricComputer[]> _computersByMetricKey;
        private Dictionary<TimeSpan, IMetricComputer[]> _computersByPublicationIntervals;

        public MetricsService(TimeSpan snapshotInterval, Func<TimeSpan> now)
        {
            _snapshotInterval = snapshotInterval;
            _now = now ?? throw new ArgumentNullException(nameof(now));

            _computersTriggerUrn = new Dictionary<Urn, IMetricComputer[]>();
            _computersByMetricKey = new Dictionary<string, IMetricComputer[]>();
            _computersByPublicationIntervals = new Dictionary<TimeSpan, IMetricComputer[]>();
        }

        public DomainEvent[] Initialize(IMetric[] metrics, IReadTimeSeries tsReader, IWriteTimeSeries tsWriter)
        {
            var metricComputerFactory = new MetricComputerFactory(tsReader, tsWriter);
            var computerRuntimeInfos = AssumeAllMetricsHaveUniqueTargetUrn(metrics)
                .Cast<Metric<MetricUrn>>()
                .Select(AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod)
                .SelectMany(metric => metricComputerFactory.Create(metric, _now()))
                .ToArray();

            _computersByMetricKey =
                (from info in computerRuntimeInfos group info.Computer by info.MetricKey)
                .ToDictionary(pair => pair.Key, pair => pair.Distinct().ToArray());

            _computersByPublicationIntervals =
                (from info in computerRuntimeInfos group info.Computer by info.PublicationPeriod)
                .ToDictionary(pair => pair.Key, pair => pair.ToArray());

            _computersTriggerUrn =
                (from info in computerRuntimeInfos
                    let computer = info.Computer
                    from trigger in info.TriggerUrns
                    group computer by trigger)
                .ToDictionary(pair => pair.Key, pair => pair.Distinct().ToArray());

            return _computersByPublicationIntervals
                .SelectMany(pair => PublishedMetrics(pair.Value, _now()))
                .ToArray();
        }

        private static IMetric[] AssumeAllMetricsHaveUniqueTargetUrn(IMetric[] metrics)
        {
            var duplicatedTargetUrns = metrics.GroupBy(m => m.TargetUrn)
                .Where(g => g.Count() > 1)
                .Select(m => m.Key.Value)
                .ToArray();

            if (duplicatedTargetUrns.Length > 0)
                throw new InvalidOperationException(
                    $"All Metric.{nameof(IMetric.TargetUrn)} must be unique. Those Metric.{nameof(IMetric.TargetUrn)} are duplicated : {string.Join(", ", duplicatedTargetUrns)}");

            return metrics;
        }

        private static Metric<MetricUrn> AssumeWindowedPeriodIsNotShorterThanPrimaryPeriod(Metric<MetricUrn> metric)
        {
            if (metric.WindowPolicy.IsSome && metric.WindowPolicy.GetValue().ToTimeSpan() <= metric.PublicationPeriod)
            {
                throw new InvalidOperationException(
                    $"Metric for {metric.TargetUrn}: Window period of Metric must be greater than primary publication period ({metric.PublicationPeriod})");
            }

            return metric;
        }

        public DomainEvent[] HandlePropertiesChanged(PropertiesChanged trigger)
        {
            foreach (var modelValue in trigger.ModelValues)
            {
                if (_computersTriggerUrn.TryGetValue(modelValue.Urn, out var computers))
                {
                    computers.ForEach(computer =>
                    {
                        computer.Update(modelValue);
                    });
                }
            }

            return Array.Empty<DomainEvent>();
        }

        public DomainEvent[] HandleSystemTicked(SystemTicked trigger)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));

            if (!trigger.IsNextDate(_snapshotInterval)) return Array.Empty<DomainEvent>();

            UpdateMetrics(trigger.At);

            var eventsToPublish = _computersByPublicationIntervals
                .Where(pair => trigger.IsNextDate(pair.Key))
                .SelectMany(pair => PublishedMetrics(pair.Value, trigger.At))
                .ToArray();

            return eventsToPublish;
        }

        private static IEnumerable<DomainEvent> PublishedMetrics(IEnumerable<IMetricComputer> computersArray, TimeSpan at)
        {
            Log.Debug("Publish metrics.", at);
            var publishersOutcome = from outcome in
                    from computer in computersArray
                    select (computer.Root, ComputerPublish: computer.Publish(at))
                where outcome.ComputerPublish.IsSome
                select (outcome.Root, ComputerPublish: outcome.ComputerPublish.GetValue());

            return publishersOutcome.Select(p => PropertiesChanged.Create(p.Root, p.ComputerPublish, at));
        }

        private void UpdateMetrics(TimeSpan at)
        {
            _computersByMetricKey
                .ForEach(pair => pair.Value
                    .ForEach(computer => computer.Update(at)));
        }
    }
}