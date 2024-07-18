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
    internal sealed class StateMonitoringVariationMeasure : StateMonitoringMeasureBase
    {
        private readonly ValuesStream _lastValueOnPreviousPeriodStream;
        private readonly ValuesStream _lastStateOnPreviousPeriodStream;
        private readonly ValueAtEnumService _valueAtEnumService;

        public StateMonitoringVariationMeasure(MetricUrn baseOutputUrn,
            Type inputEnumStateType,
            string measureName,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter,
            TimeSpan now)
            : base(baseOutputUrn, inputEnumStateType, measureName, publicationPeriod, windowPeriod, tsReader, tsWriter, now)
        {
            _lastValueOnPreviousPeriodStream = ValuesStream.Create($"{MeasureKey}$valuePreviousPeriod", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
            _lastStateOnPreviousPeriodStream = ValuesStream.Create($"{MeasureKey}$statePreviousPeriod", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
            _valueAtEnumService =
                new ValueAtEnumService(MeasureKey, publicationPeriod, windowPeriod, PeriodicService, () => InputEnumStateType, tsReader, tsWriter);
        }

        public override void Update(float newValue, TimeSpan at, Enum @enum)
        {
            Update(at);
            PeriodicService.SetSamplingEndAt(at);
            _valueAtEnumService.AddNewInputValue(newValue, at, @enum);
        }

        public override void OnPublishDone(TimeSpan now)
        {
            var curSamplingStart = PeriodicService.GetSamplingStartAt().GetValue();
            var nextSamplingStart = PeriodicService.GetSamplingStartOnNextPublish(now).GetValue();

            if (nextSamplingStart > TimeSpan.Zero && curSamplingStart <= nextSamplingStart)
                MemorizeCurrentLastInputValueForNextWindow(nextSamplingStart);

            PeriodicService.SetSamplingStartForNextPublish(now);
        }

        private void MemorizeCurrentLastInputValueForNextWindow(TimeSpan nextSamplingStart)
        {
            var atJustBeforeNextSamplingStart = new TimeSpan(nextSamplingStart.Ticks - 1);
            var last = _valueAtEnumService.GetLast(atJustBeforeNextSamplingStart);
            if (last.IsSome)
                SetLastOnPreviousPeriod(last.GetValue().Value, last.GetValue().At, last.GetValue().EnumValue.GetHashCode());
        }

        public override Property<MetricValue>[] GetItemsToPublish(TimeSpan at)
        {
            var deltaPerState = ToDeltaPerState(_valueAtEnumService.GetInputValuesForPublish(at));
            if (deltaPerState.IsNone) return Array.Empty<Property<MetricValue>>();

            return deltaPerState.GetValue()
                .Where(dic => dic.Value.IsSome)
                .Select(dic => ToIDataModelValue(dic.Key, dic.Value.GetValue(), at))
                .ToArray();
        }

        private Option<Dictionary<Enum, Option<float>>> ToDeltaPerState(ValueAtEnum[] inputValues)
        {
            var previousItem = GetLastOnPreviousPeriod()
                .Match(Option<ValueAtEnum>.None,
                    last =>
                    {
                        var samplingStartAt = PeriodicService.GetSamplingStartAt().GetValue();
                        return last.At <= samplingStartAt
                            ? last
                            : Option<ValueAtEnum>.None();
                    });

            var opDeltaPerState = CreateAndInitDeltaPerState();
            if (opDeltaPerState.IsNone) return Option<Dictionary<Enum, Option<float>>>.None();

            var deltaPerState = opDeltaPerState.GetValue();

            if (previousItem.IsSome && inputValues.IsEmpty())
                return deltaPerState.ToDictionary(dic => dic.Key, dic => Option<float>.Some(0f));

            var thereIsAtLeastOneDeltaComputed = false;
            inputValues.ForEach(curItem =>
            {
                previousItem.Tap(previous =>
                {
                    var newDelta = curItem.Value - previous.Value;
                    var deltaValue = deltaPerState[curItem.EnumValue];
                    deltaValue.Tap(old => newDelta += old);
                    deltaPerState[curItem.EnumValue] = newDelta;
                    thereIsAtLeastOneDeltaComputed = true;
                });

                previousItem = curItem;
            });

            return thereIsAtLeastOneDeltaComputed
                ? ReplaceAllNoneValueToZero(deltaPerState)
                : opDeltaPerState;
        }

        private static Option<Dictionary<Enum, Option<float>>> ReplaceAllNoneValueToZero(Dictionary<Enum, Option<float>> deltaPerState)
        {
            return deltaPerState.Select(dic =>
            {
                var value = dic.Value.IsNone
                    ? 0
                    : dic.Value;

                return (dic.Key, value);
            }).ToDictionary(pair => pair.Key, pair => pair.value);
        }

        private Option<Dictionary<Enum, Option<float>>> CreateAndInitDeltaPerState()
            => Enum.GetValues(InputEnumStateType)
                .Cast<Enum>()
                .ToDictionary(enumValue => enumValue, enumValue => Option<float>.None());

        private Option<ValueAtEnum> GetLastOnPreviousPeriod()
        {
            var lastValue = _lastValueOnPreviousPeriodStream.Last();
            var lastState = _lastStateOnPreviousPeriodStream.Last();
            if (lastValue.IsNone || lastState.IsNone) return Option<ValueAtEnum>.None();

            var opEnumValue = ValueAtEnumService.ToEnum(InputEnumStateType, (int) lastState.GetValue().Value);
            return opEnumValue.IsNone
                ? Option<ValueAtEnum>.None()
                : new ValueAtEnum(lastValue.GetValue().Value, lastValue.GetValue().At, opEnumValue.GetValue());
        }

        private void SetLastOnPreviousPeriod(float value, TimeSpan at, float enumCode)
        {
            _lastValueOnPreviousPeriodStream.Write(value, at);
            _lastStateOnPreviousPeriodStream.Write(enumCode, at);
        }

        private Property<MetricValue> ToIDataModelValue(Enum @enum, float deltaValue, TimeSpan at)
        {
            var outputUrn = MetricUrn.Build(BaseOutputUrn, @enum.ToString(), MeasureName);
            var startAt = PeriodicService.GetSamplingStartAt().GetValue();
            var endAt = PeriodicService.GetSamplingEndAt().GetValue();
            return Property<MetricValue>.Create(outputUrn, new MetricValue(deltaValue, startAt, endAt), at);
        }
    }
}