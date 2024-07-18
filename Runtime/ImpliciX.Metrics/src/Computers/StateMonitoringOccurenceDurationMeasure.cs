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
    internal sealed class StateMonitoringOccurenceDurationMeasure : StateMonitoringMeasureBase
    {
        private readonly ValuesStream _lastStateOnPreviousPeriodStream;

        public StateMonitoringOccurenceDurationMeasure(MetricUrn baseOutputUrn,
            Type inputEnumStateType,
            string measureName,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter,
            TimeSpan now)
            : base(baseOutputUrn, inputEnumStateType, measureName, publicationPeriod, windowPeriod, tsReader, tsWriter, now)
        {
            _lastStateOnPreviousPeriodStream = ValuesStream.Create($"{MeasureKey}$statePreviousPeriod", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
        }

        public override void Update(float _, TimeSpan at, Enum @enum)
        {
            Update(at);
            PeriodicService.SetSamplingEndAt(at);

            var enumValue = @enum.GetHashCode();
            PeriodicService.AddNewInputValue(enumValue, at);
        }

        public override void OnPublishDone(TimeSpan now)
        {
            var previousLastInput = PeriodicService.GetLastInput();
            PeriodicService.SetSamplingStartForNextPublish(now);

            if (previousLastInput.IsNone) return;
            var previousLast = previousLastInput.GetValue();
            _lastStateOnPreviousPeriodStream.Write(previousLast.Value, now);

            if (PeriodicService.GetAt(now).IsSome) return;
            PeriodicService.AddToInputValues(new FloatAt(previousLast.Value, now));
        }

        public override Property<MetricValue>[] GetItemsToPublish(TimeSpan at)
        {
            var samplingEndAt = PeriodicService.GetSamplingEndAt();
            if (samplingEndAt.IsNone) return Array.Empty<Property<MetricValue>>();

            var valuePerState = ToOccurenceDurationPerState(GetInputValuesForPublish(at));
            if (valuePerState.IsNone) return Array.Empty<Property<MetricValue>>();

            return valuePerState.GetValue()
                .SelectMany(dic =>
                {
                    return new[]
                    {
                        ToIDataModelValue(MetricUrn.OCCURRENCE, dic.Key, dic.Value.Occurence, at),
                        ToIDataModelValue(MetricUrn.DURATION, dic.Key, (float) dic.Value.Duration.TotalSeconds, at)
                    };
                })
                .ToArray();
        }

        private FloatAt[] GetInputValuesForPublish(TimeSpan publishAt)
            => PeriodicService.GetInputsForPublish(publishAt)
                .Match(Array.Empty<FloatAt>, values => values.OrderBy(o => o.At).ToArray());

        private Option<Dictionary<Enum, OccurenceDuration>> ToOccurenceDurationPerState(FloatAt[] inputValues)
        {
            var opValuePerState = CreateAndInitValuePerState();
            if (opValuePerState.IsNone) return Option<Dictionary<Enum, OccurenceDuration>>.None();

            var valuePerEnum = opValuePerState.GetValue();
            var samplingStartAt = PeriodicService.GetSamplingStartAt().GetValue();

            PreservingPreviousStateOccurence(inputValues, valuePerEnum);

            var previousState = Option<Enum>.None();
            var previousItemAt = samplingStartAt;
            inputValues.ForEach(curItem =>
            {
                ValueAtEnumService.ToEnum(InputEnumStateType, (int) curItem.Value).Tap(state =>
                {
                    if (previousState.IsSome && Equals(previousState.GetValue(), state))
                        return;

                    var current = valuePerEnum[state];
                    valuePerEnum[state] = new OccurenceDuration(current.Occurence + 1, current.Duration);

                    previousState.Tap(prevState => AddDurationToState(valuePerEnum, prevState, curItem.At - previousItemAt));
                    previousItemAt = curItem.At;
                    previousState = state;
                });
            });

            return previousState.IsSome
                ? AddDurationToState(valuePerEnum, previousState.GetValue(), PeriodicService.GetSamplingEndAt().GetValue() - previousItemAt)
                : Option<Dictionary<Enum, OccurenceDuration>>.None();
        }

        private void PreservingPreviousStateOccurence(FloatAt[] inputValues, Dictionary<Enum, OccurenceDuration> valuePerEnum)
        {
            _lastStateOnPreviousPeriodStream.Last().Tap(previous =>
            {
                var previousStateWasReceived = inputValues.Any(o => (int) o.Value == (int) previous.Value);
                if (previousStateWasReceived) return;

                var valueOnStartAt = inputValues.Where(o => o.At == previous.At).ToArray();
                if (valueOnStartAt.Any() && (int) valueOnStartAt.First().Value != (int) previous.Value)
                {
                    ValueAtEnumService.ToEnum(InputEnumStateType, (int) previous.Value).Tap(state =>
                    {
                        var current = valuePerEnum[state];
                        valuePerEnum[state] = new OccurenceDuration(current.Occurence + 1, current.Duration);
                    });
                }
            });
        }

        private static Dictionary<Enum, OccurenceDuration> AddDurationToState(Dictionary<Enum, OccurenceDuration> dic, Enum state, TimeSpan durationToAdd)
        {
            if (dic == null) throw new ArgumentNullException(nameof(dic));
            var item = dic[state];
            var duration = item.Duration + durationToAdd;
            dic[state] = new OccurenceDuration(item.Occurence, duration);
            return dic;
        }

        private Option<Dictionary<Enum, OccurenceDuration>> CreateAndInitValuePerState()
            => Enum.GetValues(InputEnumStateType)
                .Cast<Enum>()
                .ToDictionary(enumValue => enumValue, enumValue => new OccurenceDuration(0f, TimeSpan.Zero));

        private Property<MetricValue> ToIDataModelValue(string measureName, Enum @enum, float value, TimeSpan at)
        {
            var outputUrn = MetricUrn.Build(BaseOutputUrn, @enum.ToString(), measureName);
            var startAt = PeriodicService.GetSamplingStartAt().GetValue();
            var endAt = PeriodicService.GetSamplingEndAt().GetValue();
            return Property<MetricValue>.Create(outputUrn, new MetricValue(value, startAt, endAt), at);
        }

        private readonly struct OccurenceDuration
        {
            public float Occurence { get; }
            public TimeSpan Duration { get; }

            public OccurenceDuration(float occurence, TimeSpan duration)
            {
                Occurence = occurence;
                Duration = duration;
            }
        }
    }
}