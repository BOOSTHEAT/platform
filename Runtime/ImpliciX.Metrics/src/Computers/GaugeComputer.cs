using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Metrics.Computers
{
    public class GaugeComputer : IMetricComputer
    {
        private readonly MetricUrn _outputUrn;
        private readonly PeriodicComputerService _periodicService;
        public Urn Root { get; }

        public GaugeComputer(
            MetricUrn outputUrn,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter,
            TimeSpan now
        )
        {
            _outputUrn = outputUrn ?? throw new ArgumentNullException(nameof(outputUrn));
            Root = _outputUrn;

            _periodicService =
                new PeriodicComputerService(
                    outputUrn,
                    ValuesStreamPeriod.KeepLatestOnly,
                    ValuesStreamPeriod.KeepLatestOnly,
                    tsReader,
                    tsWriter,
                    now);
        }

        public void Update(IDataModelValue modelValue)
        {
            var value = ((IFloat) modelValue.ModelValue()).ToFloat();
            _periodicService.AddNewInputValue(value, modelValue.At);
            _periodicService.SetSamplingEndAt(modelValue.At);
        }

        public void Update(TimeSpan at) => _periodicService.SetSamplingEndAt(at);

        public Option<Property<MetricValue>[]> Publish(TimeSpan now)
        {
            var curSamplingStart = _periodicService.GetSamplingStartAt().GetValue();
            _periodicService.SetSamplingStartForNextPublish(now);

            var samplingEndAt = _periodicService.GetSamplingEndAt();
            if (samplingEndAt.IsNone) return Option<Property<MetricValue>[]>.None();

            var lastInput = _periodicService.GetLastInput();
            return lastInput.Match(
                Option<Property<MetricValue>[]>.None,
                last => new[]
                {
                    Property<MetricValue>.Create(_outputUrn, new MetricValue(last.Value, curSamplingStart, samplingEndAt.GetValue()), now)
                }
            );
        }
    }
}
