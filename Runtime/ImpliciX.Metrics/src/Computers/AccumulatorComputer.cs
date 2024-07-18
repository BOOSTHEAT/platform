using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics.Computers
{
    internal sealed class AccumulatorComputer : IMetricComputer
    {
        private readonly PeriodicComputerService _periodicService;
        private readonly ValuesStream _accumulatedValueStream;
        private readonly ValuesStream _samplesCountStream;
        public Urn Root { get; }

        public AccumulatorComputer(MetricUrn outputUrn,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter,
            TimeSpan now)
        {
            Root = outputUrn;
            _periodicService = new PeriodicComputerService(outputUrn, windowPeriod ?? publicationPeriod, publicationPeriod, tsReader, tsWriter, now);
            _accumulatedValueStream = ValuesStream.Create($"{outputUrn}$accumulatedValue", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
            _samplesCountStream = ValuesStream.Create($"{outputUrn}$samplesCount", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
        }

        public void Update(IDataModelValue modelValue)
        {
            var value = ((IFloat) modelValue.ModelValue()).ToFloat();
            _periodicService.SetSamplingEndAt(modelValue.At);
            _periodicService.AddNewInputValue(value, modelValue.At);
        }

        public void Update(TimeSpan at) => _periodicService.SetSamplingEndAt(at);

        public Option<Property<MetricValue>[]> Publish(TimeSpan now)
        {
            var samplingEndAt = _periodicService.GetSamplingEndAt();
            if (samplingEndAt.IsNone) return Option<Property<MetricValue>[]>.None();

            var (accumulatedValue, samplesCount) = GetGetAccumulatedValueAndCount();
            var samplingStartAt = _periodicService.GetSamplingStartAt().GetValue();
            var toPublish = new[]
            {
                Property<MetricValue>.Create(MetricUrn.BuildAccumulatedValue(Root),
                    new MetricValue(accumulatedValue, samplingStartAt, samplingEndAt.GetValue()), now),
                Property<MetricValue>.Create(MetricUrn.BuildSamplesCount(Root),
                    new MetricValue(samplesCount, samplingStartAt, samplingEndAt.GetValue()), now)
            };

            _periodicService.SetSamplingStartForNextPublish(now);
            if (_periodicService.IsPeriodEnding(now))
                ResetAccumulatorValues(now);

            return toPublish;
        }

        private void ResetAccumulatorValues(TimeSpan at)
        {
            _accumulatedValueStream.Write(0, at);
            _samplesCountStream.Write(0, at);
        }

        private (float acc, float count) GetGetAccumulatedValueAndCount()
        {
            var allInputs = _periodicService.GetAllInputs();
            return allInputs.IsNone
                ? (0, 0)
                : (
                    allInputs.GetValue().Sum(i => i.Value),
                    allInputs.GetValue().Length
                );
        }
    }
}