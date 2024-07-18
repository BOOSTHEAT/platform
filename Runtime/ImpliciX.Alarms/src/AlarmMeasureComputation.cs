using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Alarms
{
    public class AlarmMeasureComputation : AlarmBase
    {

        private Presence _presence;
        private MeasureStatus _status;

        public AlarmMeasureComputation(AlarmNode alarm)
        {
            _alarmUrn = alarm.state;
            _presence = Presence.Disabled;
            _status = MeasureStatus.Success;
        }

        public Option<DomainEvent> OnSensorStatus(IDataModelValue value, ModelFactory modelFactory, TimeSpan now)
        {
            var status = (MeasureStatus) value.ModelValue();
            if (status != _status)
            {
                _status = status;
                return CreateDomainEvent(modelFactory, now)(ComputeAlarmState());
            }
            else
                return Option<DomainEvent>.None();
        }

        public Option<DomainEvent> OnAlarmPresence(IDataModelValue value, ModelFactory modelFactory, TimeSpan now)
        {
            var presence = (Presence) value.ModelValue();
            if (presence != _presence)
            {
                _status = presence == Presence.Enabled ? MeasureStatus.Success : MeasureStatus.Failure;
                _presence = presence;
                return CreateDomainEvent(modelFactory, now)(ComputeAlarmState());
            }
            else
                return Option<DomainEvent>.None();
        }

        private Func<AlarmState, DomainEvent> CreateDomainEvent(ModelFactory modelFactory, TimeSpan now) =>
            alarmState => PropertiesChanged.Create(new[]
                {
                    (IDataModelValue) modelFactory.CreateWithLog(_alarmUrn, alarmState, now).Value
                },
                now
            );

        private AlarmState ComputeAlarmState()
        {
            if (_presence == Presence.Disabled) return AlarmState.Inactive;
            return _status switch
            {
                MeasureStatus.Success => AlarmState.Inactive,
                MeasureStatus.Failure => AlarmState.Active,
                _ => throw new ArgumentOutOfRangeException(nameof(_status), _status, null)
            };
        }
    }
}