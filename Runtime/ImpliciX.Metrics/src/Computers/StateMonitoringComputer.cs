using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;
using static ImpliciX.Language.Model.MetricUrn;

namespace ImpliciX.Metrics.Computers
{
    internal sealed class StateMonitoringComputer : IMetricComputer
    {
        private readonly Type _inputEnumStateType;
        private readonly TimeSpan _publicationPeriod;
        private readonly TimeSpan? _windowPeriod;
        private readonly IReadTimeSeries _tsReader;
        private readonly IWriteTimeSeries _tsWriter;

        private readonly Dictionary<Urn, float> _measuresStore = new Dictionary<Urn, float>();

        private Enum? _previousState;
        private readonly Dictionary<Urn, MeasureRuntimeInfo[]> _measureInfosByInputUrn;
        private readonly Dictionary<Urn, StateMonitoringMeasureBase[]> _measureByInputUrn;
        private readonly StateMonitoringOccurenceDurationMeasure _occurenceDurationMeasure;
        public Urn Root { get; }

        public StateMonitoringComputer(MetricUrn outputUrn,
            Type inputEnumStateType,
            TimeSpan publicationPeriod,
            TimeSpan? windowPeriod,
            MeasureRuntimeInfo[] measureRuntimeInfos,
            IReadTimeSeries tsReader, IWriteTimeSeries tsWriter, TimeSpan now)
        {
            if (inputEnumStateType == null) throw new ArgumentNullException(nameof(inputEnumStateType));
            if (measureRuntimeInfos == null) throw new ArgumentNullException(nameof(measureRuntimeInfos));

            if (inputEnumStateType != typeof(Enum) && !inputEnumStateType.IsEnum)
                throw new ArgumentException("Type provided must be an Enum.", nameof(inputEnumStateType));

            _inputEnumStateType = inputEnumStateType;
            _publicationPeriod = publicationPeriod;
            _windowPeriod = windowPeriod;
            _tsReader = tsReader ?? throw new ArgumentNullException(nameof(tsReader));
            _tsWriter = tsWriter ?? throw new ArgumentNullException(nameof(tsWriter));

            Root = outputUrn;

            _measureInfosByInputUrn = measureRuntimeInfos
                .Prepend(new MeasureRuntimeInfo(StateMonitoringMeasureKind.OccurenceDuration, OCCURRENCE, Root))
                .GroupBy(info => info.InputUrn)
                .ToDictionary(info => info.Key, info => info.ToArray());

            _measureByInputUrn = _measureInfosByInputUrn
                .ToDictionary(
                    dic => dic.Key,
                    dic => dic.Value
                        .Select(info => MeasureFactory(info.MeasureKind, info.MeasureName, now).GetValue())
                        .ToArray()
                );

            _occurenceDurationMeasure = _measureByInputUrn
                .SelectMany(dic => dic.Value.OfType<StateMonitoringOccurenceDurationMeasure>())
                .Single();
        }

        #region Update

        public void Update(IDataModelValue modelValue)
        {
            var newValue = modelValue.ModelValue();
            if (newValue is SubsystemState subsystemState)
                newValue = subsystemState.State;

            switch (newValue)
            {
                case Enum enumValue:
                    IncrementOccurrenceCounter(enumValue, modelValue.At);
                    Update(modelValue.At);
                    _previousState = enumValue;
                    break;

                case IFloat measureValue:
                    OnFloatValueReceived(modelValue, measureValue);
                    Update(modelValue.At);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"{nameof(StateMonitoringComputer)} can not be updated with '{modelValue.Urn}' because its type '{modelValue.ModelValue().GetType()}' is not allowed");
            }
        }

        public void Update(TimeSpan at) => UpdateCurrentTimeToAllMeasures(at);

        private void UpdateCurrentTimeToAllMeasures(TimeSpan at)
            => _measureByInputUrn.SelectMany(o => o.Value).ForEach(m => m.Update(at));

        private void OnFloatValueReceived(IDataModelValue modelValue, IFloat measureValue)
        {
            if (_previousState is null) return;
            if (!_measureInfosByInputUrn.TryGetValue(modelValue.Urn, out var infos)) return;

            infos.ForEach(info => UpdateMeasure(info, modelValue));
            StoreMeasureValue(modelValue.Urn, measureValue.ToFloat());
        }

        private void StoreMeasureValue(Urn key, float measureValue)
            => _measuresStore.AddOrUpdate(key, measureValue, _ => measureValue);

        private Option<StateMonitoringMeasureBase> MeasureFactory(StateMonitoringMeasureKind metricKind, string measureName, TimeSpan now)
            => metricKind switch
            {
                StateMonitoringMeasureKind.Variation => new StateMonitoringVariationMeasure(Build(Root), _inputEnumStateType, measureName,
                    _publicationPeriod, _windowPeriod, _tsReader, _tsWriter, now),
                StateMonitoringMeasureKind.Accumulator => new StateMonitoringAccumulatorMeasure(Build(Root), _inputEnumStateType, measureName,
                    _publicationPeriod, _windowPeriod, _tsReader, _tsWriter, now),
                StateMonitoringMeasureKind.OccurenceDuration => new StateMonitoringOccurenceDurationMeasure(Build(Root), _inputEnumStateType, measureName,
                    _publicationPeriod, _windowPeriod, _tsReader, _tsWriter, now),
                _ => Option<StateMonitoringMeasureBase>.None()
            };

        private void UpdateMeasure(MeasureRuntimeInfo info, IDataModelValue modelValue)
        {
            if (_previousState is null) return;

            var measures = _measureByInputUrn[info.InputUrn];
            var floatValue = ((IFloat) modelValue.ModelValue()).ToFloat();
            measures.ForEach(m => m.Update(floatValue, modelValue.At, _previousState));
        }

        private void IncrementOccurrenceCounter(Enum currentState, TimeSpan at)
        {
            if (currentState.Equals(_previousState)) return;
            _occurenceDurationMeasure.Update(1, at, currentState);
        }

        #endregion

        #region Publish

        public Option<Property<MetricValue>[]> Publish(TimeSpan now)
        {
            var properties = _measureByInputUrn
                .SelectMany(m => m.Value)
                .SelectMany(m => m.GetItemsToPublish(now))
                .ToArray();

            OnPublishDone(now);
            return properties.IsEmpty()
                ? Option<Property<MetricValue>[]>.None()
                : properties;
        }

        private void OnPublishDone(TimeSpan now)
        {
            _measureByInputUrn
                .SelectMany(dic => dic.Value)
                .ForEach(measure => measure.OnPublishDone(now));
        }

        #endregion
    }
}