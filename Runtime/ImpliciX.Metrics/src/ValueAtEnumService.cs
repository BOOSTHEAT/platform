using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Metrics.Computers;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics
{
    internal sealed class ValueAtEnumService
    {
        private readonly PeriodicComputerService _periodicService;
        private readonly Func<Option<Type>> _getEnumType;
        private readonly IReadTimeSeries _tsReader;
        private readonly ValuesStream _inputStateStream;

        public ValueAtEnumService(
            string measureKey,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            PeriodicComputerService periodicComputerService,
            Func<Option<Type>> getEnumType,
            IReadTimeSeries tsReader,
            IWriteTimeSeries tsWriter)
        {
            _periodicService = periodicComputerService ?? throw new ArgumentNullException(nameof(periodicComputerService));
            _getEnumType = getEnumType ?? throw new ArgumentNullException(nameof(getEnumType));
            _tsReader = tsReader;
            _inputStateStream = ValuesStream.Create($"{measureKey}$inputStateStream", tsReader, tsWriter, windowPeriod ?? publicationPeriod);
        }

        public void AddNewInputValue(float newValue, TimeSpan at, Enum @enum)
        {
            _periodicService.AddNewInputValue(newValue, at);
            AddInputState(@enum, at);
        }

        private void AddInputState(Enum @enum, TimeSpan at)
        {
            var enumId = @enum.GetHashCode();

            _inputStateStream.Last().Tap(
                () => _inputStateStream.Write(enumId, at),
                last =>
                {
                    var lastEnumCode = (int) last.Value;
                    if (lastEnumCode != enumId)
                        _inputStateStream.Write(enumId, at);
                });
        }

        public Option<ValueAtEnum> GetLast(TimeSpan? upTo = null)
        {
            var last = _periodicService.GetLastInput(upTo);
            var lastState = _inputStateStream.Last(upTo);
            if (last.IsNone || lastState.IsNone) return Option<ValueAtEnum>.None();

            var state = ToEnum(_getEnumType().GetValue(), (int) lastState.GetValue().Value);
            return new ValueAtEnum(last.GetValue().Value, last.GetValue().At, state.GetValue());
        }

        public ValueAtEnum[] GetInputValuesForPublish(TimeSpan publishAt)
        {
            if (_getEnumType().IsNone) return Array.Empty<ValueAtEnum>();

            var opValueAts = _periodicService.GetInputsForPublish(publishAt);
            var opStates = _inputStateStream.ReadAllPeriodValues();
            if (opValueAts.IsNone || opStates.IsNone) return Array.Empty<ValueAtEnum>();

            var result = new List<ValueAtEnum>();
            var inputStates = opStates.GetValue().Select(input => new FloatAt(input.Value, input.At)).ToArray();
            opValueAts.GetValue().ForEach(valueAt =>
            {
                GetStateWhenAt(inputStates, valueAt.At)
                    .Tap(state => result.Add(new ValueAtEnum(valueAt.Value, valueAt.At, state)));
            });

            return result.ToArray();
        }

        private Option<Enum> GetStateWhenAt(FloatAt[] inputStates, TimeSpan at)
        {
            var enumType = _getEnumType();
            if (enumType.IsNone) return Option<Enum>.None();
            if (inputStates.IsEmpty()) return Option<Enum>.None();

            var states = inputStates
                .Where(o => o.At <= at)
                .OrderBy(o => o.At)
                .ToArray();

            if (states.IsEmpty()) return Option<Enum>.None();

            var stateCode = (int) states.Last().Value;
            return ToEnum(enumType.GetValue(), stateCode)
                .Match(
                    Option<Enum>.None,
                    state => state
                );
        }

        public static Option<Enum> ToEnum(Type enumType, int enumHashCode)
        {
            foreach (Enum item in Enum.GetValues(enumType))
            {
                if (item.GetHashCode() == enumHashCode)
                    return item;
            }

            return Option<Enum>.None();
        }
    }
}