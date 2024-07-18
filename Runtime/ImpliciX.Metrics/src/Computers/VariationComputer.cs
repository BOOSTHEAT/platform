using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics.Computers
{
    internal sealed class VariationComputer : IMetricComputer
    {
        private readonly ValuesStream _lastOnPreviousPublishStream;
        private readonly PeriodicComputerService _periodicService;
        private readonly MetricUrn _outputMetricUrn;
        public Urn Root { get; }

        public VariationComputer(MetricUrn outputUrn,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            IReadTimeSeries tsReader, IWriteTimeSeries tsWriter, TimeSpan now)
        {
            _outputMetricUrn = outputUrn ?? throw new ArgumentNullException(nameof(outputUrn));
            Root = outputUrn;
            _periodicService = new PeriodicComputerService(outputUrn, windowPeriod ?? publicationPeriod, publicationPeriod, tsReader, tsWriter, now);

            _lastOnPreviousPublishStream =
                ValuesStream.Create($"{outputUrn}$lastOnPreviousPublish", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
        }

        public void Update(IDataModelValue modelValue)
        {
            var value = ((IFloat) modelValue.ModelValue()).ToFloat();
            Update(modelValue.At);
            _periodicService.AddNewInputValue(value, modelValue.At);
        }

        public void Update(TimeSpan at) => _periodicService.SetSamplingEndAt(at);

        public Option<Property<MetricValue>[]> Publish(TimeSpan now)
        {
            var samplingEndAt = _periodicService.GetSamplingEndAt();
            if (samplingEndAt.IsNone) return Option<Property<MetricValue>[]>.None();

            var curSamplingStart = _periodicService.GetSamplingStartAt().GetValue();
            var delta = ComputeDeltaValue();
            var toPublish = new[]
            {
                Property<MetricValue>.Create(_outputMetricUrn, new MetricValue(delta, curSamplingStart, samplingEndAt.GetValue()), now)
            };

            var nextSamplingStart = _periodicService.GetSamplingStartOnNextPublish(now).GetValue();
            if (nextSamplingStart > TimeSpan.Zero && curSamplingStart <= nextSamplingStart)
                MemorizeCurrentLastInputValueForNextWindow(nextSamplingStart);

            _periodicService.SetSamplingStartForNextPublish(now);
            return toPublish;
        }

        private float ComputeDeltaValue()
        {
            var lastValue = _periodicService.GetLastInput();
            if (lastValue.IsNone) return 0;

            return GetLastOnPreviousPublish().Match(
                () => _periodicService.GetFirstInput().Match(() => 0, first => lastValue.GetValue().Value - first.Value),
                lastOnPreviousPublish => lastValue.GetValue().Value - lastOnPreviousPublish.Value
            );
        }

        private void MemorizeCurrentLastInputValueForNextWindow(TimeSpan nextSamplingStart)
        {
            var atJustBeforeNextSamplingStart = new TimeSpan(nextSamplingStart.Ticks - 1);
            var lastValue = _periodicService.GetLastInput(atJustBeforeNextSamplingStart);
            if (lastValue.IsSome)
                SetLastOnPreviousPublish(lastValue.GetValue());
        }

        private Option<FloatAt> GetLastOnPreviousPublish()
            => _lastOnPreviousPublishStream.Last().Match(Option<FloatAt>.None, data => new FloatAt(data.Value, data.At));

        private void SetLastOnPreviousPublish(FloatAt floatAt)
            => _lastOnPreviousPublishStream.Write(floatAt.Value, floatAt.At);
    }
}