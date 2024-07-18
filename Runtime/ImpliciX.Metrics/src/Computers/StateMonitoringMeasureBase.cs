using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics.Computers
{
    internal abstract class StateMonitoringMeasureBase
    {
        protected Type InputEnumStateType { get; }
        protected PeriodicComputerService PeriodicService { get; }
        protected MetricUrn BaseOutputUrn { get; private set; }
        protected string MeasureKey { get; }
        protected string MeasureName { get; }

        protected StateMonitoringMeasureBase(MetricUrn baseOutputUrn,
            Type inputEnumStateType,
            string measureName,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter,
            TimeSpan now)
        {
            InputEnumStateType = inputEnumStateType ?? throw new ArgumentNullException(nameof(inputEnumStateType));
            BaseOutputUrn = baseOutputUrn ?? throw new ArgumentNullException(nameof(baseOutputUrn));
            MeasureName = measureName ?? throw new ArgumentNullException(nameof(measureName));

            MeasureKey = CreateMeasureKey(baseOutputUrn, measureName);
            PeriodicService = new PeriodicComputerService(MeasureKey, windowPeriod ?? publicationPeriod, publicationPeriod, tsReader, tsWriter, now);
        }

        private static string CreateMeasureKey(MetricUrn baseOutputUrn, string measureName) => $"{baseOutputUrn}${measureName}";

        public abstract void Update(float newValue, TimeSpan now, Enum @enum);
        public void Update(TimeSpan at) => PeriodicService.SetSamplingEndAt(at);
        public abstract void OnPublishDone(TimeSpan now);
        public abstract Property<MetricValue>[] GetItemsToPublish(TimeSpan now);
    }

    internal enum StateMonitoringMeasureKind
    {
        OccurenceDuration = 1,
        Variation = 2,
        Accumulator = 3
    }

    internal static class MetricKindExtensions
    {
        public static StateMonitoringMeasureKind ToSateMonitoringMeasureKind(this MetricKind metricKind)
            => metricKind switch
            {
                MetricKind.Variation => StateMonitoringMeasureKind.Variation,
                MetricKind.SampleAccumulator => StateMonitoringMeasureKind.Accumulator,
                _ => throw new InvalidOperationException($"'{metricKind.ToString()}' metric kind is unknown for {nameof(StateMonitoringMeasureKind)}")
            };
    }
}