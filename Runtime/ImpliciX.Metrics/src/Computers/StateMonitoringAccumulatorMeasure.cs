using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics.Computers
{
    internal sealed class StateMonitoringAccumulatorMeasure : StateMonitoringMeasureBase
    {
        private readonly ValueAtEnumService _valueAtEnumService;

        public StateMonitoringAccumulatorMeasure(MetricUrn baseOutputUrn,
            Type inputEnumStateType,
            string measureName,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter,
            TimeSpan now)
            : base(baseOutputUrn, inputEnumStateType, measureName, publicationPeriod, windowPeriod, tsReader, tsWriter, now)
        {
            _valueAtEnumService =
                new ValueAtEnumService(MeasureKey, publicationPeriod, windowPeriod, PeriodicService, () => InputEnumStateType, tsReader, tsWriter);
        }

        public override void Update(float newValue, TimeSpan at, Enum @enum)
        {
            Update(at);
            PeriodicService.SetSamplingEndAt(at);
            _valueAtEnumService.AddNewInputValue(newValue, at, @enum);
        }

        public override void OnPublishDone(TimeSpan now) => PeriodicService.SetSamplingStartForNextPublish(now);

        public override Property<MetricValue>[] GetItemsToPublish(TimeSpan at)
        {
            var accPerState = ToAccumulatorPerState(_valueAtEnumService.GetInputValuesForPublish(at));
            if (accPerState.IsNone) return Array.Empty<Property<MetricValue>>();

            return accPerState.GetValue()
                .SelectMany(dic => ToIDataModelValue(dic.Key, dic.Value.AccumulatedValue, dic.Value.SamplesCount, at))
                .ToArray();
        }

        private Option<Dictionary<Enum, AccumulatorItem>> ToAccumulatorPerState(ValueAtEnum[] inputValues)
        {
            var opAccPerState = CreateAndInitAccumulatorDeltaPerState();
            if (opAccPerState.IsNone) return Option<Dictionary<Enum, AccumulatorItem>>.None();

            var accPerState = opAccPerState.GetValue();

            inputValues.ForEach(curItem =>
            {
                var newAcc = accPerState[curItem.EnumValue].AccumulatedValue;
                newAcc += curItem.Value;
                var newCount = accPerState[curItem.EnumValue].SamplesCount + 1;
                accPerState[curItem.EnumValue] = new AccumulatorItem(newAcc, newCount);
            });

            return opAccPerState;
        }

        private Option<Dictionary<Enum, AccumulatorItem>> CreateAndInitAccumulatorDeltaPerState()
            => Enum.GetValues(InputEnumStateType)
                .Cast<Enum>()
                .ToDictionary(enumValue => enumValue, enumValue => new AccumulatorItem(0, 0));

        private IEnumerable<Property<MetricValue>> ToIDataModelValue(Enum @enum, float accValue, float accCount, TimeSpan at)
        {
            var outputUrn = MetricUrn.Build(BaseOutputUrn, @enum.ToString(), MeasureName);
            var startAt = PeriodicService.GetSamplingStartAt().GetValue();
            var endAt = PeriodicService.GetSamplingEndAt().GetValue();
            return new[]
            {
                Property<MetricValue>.Create(MetricUrn.BuildSamplesCount(outputUrn), new MetricValue(accCount, startAt, endAt), at),
                Property<MetricValue>.Create(MetricUrn.BuildAccumulatedValue(outputUrn), new MetricValue(accValue, startAt, endAt), at)
            };
        }
    }

    internal readonly struct AccumulatorItem
    {
        public float AccumulatedValue { get; }
        public float SamplesCount { get; }

        public AccumulatorItem(float value, float samplesCount)
        {
            AccumulatedValue = value;
            SamplesCount = samplesCount;
        }
    }
}