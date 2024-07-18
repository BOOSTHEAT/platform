using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Alarms
{
    internal enum State
    {
        Normal,
        Abnormal,
        ReadyToReset,
    }

    public class AlarmTriggerComputation : AlarmBase
    {
        private Presence _presence;
        private State _currentState;

        private readonly PropertyUrn<AlarmReset> _readyToResetUrn;
        private readonly Func<IDataModelValue, bool> _normalToAbnormal;
        private readonly Func<IDataModelValue, bool> _abnormalToReadyToReset;

        public AlarmTriggerComputation(AlarmNode alarm, Func<IDataModelValue, bool>[] predicates)
        {
            _presence = Presence.Disabled;
            _currentState = State.Normal;

            _normalToAbnormal = predicates[0];
            _abnormalToReadyToReset = predicates[1];
            _readyToResetUrn = alarm.ready_to_reset;
            _alarmUrn = alarm.state;
        }

        public Option<DomainEvent> OnFunctionalState(IDataModelValue value, ModelFactory factory, TimeSpan now)
        {
            switch (_currentState, _presence)
            {
                case (State.Normal, Presence.Enabled) when _normalToAbnormal(value):
                    _currentState = State.Abnormal;
                    return CreateOptionOfDomainEvent(now,
                        CreateAlarmStateEvent(factory, now)(AlarmState.Active));
                case (State.Abnormal, Presence.Enabled) when _abnormalToReadyToReset(value):
                    _currentState = State.ReadyToReset;
                    return CreateOptionOfDomainEvent(now,
                        CreateAlarmReadyToResetEvent(factory, now)(AlarmReset.Yes));
                default:
                    return Option<DomainEvent>.None();
            }
        }

        public Option<DomainEvent> OnAlarmPresence(IDataModelValue value, ModelFactory factory, TimeSpan now)
        {
            var newPresence = (Presence) value.ModelValue();
            switch (_presence, newPresence)
            {
                case (Presence.Enabled, Presence.Disabled):
                case (Presence.Disabled, Presence.Enabled):
                    var isCurrentlyReadyToReset = _currentState == State.ReadyToReset;
                    _currentState = State.Normal;
                    _presence = newPresence;
                    if (isCurrentlyReadyToReset)
                        return CreateOptionOfDomainEvent(now,
                            CreateAlarmStateEvent(factory, now)(AlarmState.Inactive),
                            CreateAlarmReadyToResetEvent(factory, now)(AlarmReset.No)
                        );
                    else
                        return CreateOptionOfDomainEvent(now,
                            CreateAlarmStateEvent(factory, now)(AlarmState.Inactive));
                default:
                    return Option<DomainEvent>.None();
            }
        }

        public DomainEvent[] OnResetCommand(ModelFactory factory, TimeSpan now)
        {
            if (_currentState == State.ReadyToReset)
            {
                _currentState = State.Normal;
                return CreateDomainEvent(now,
                    CreateAlarmStateEvent(factory, now)(AlarmState.Inactive),
                    CreateAlarmReadyToResetEvent(factory, now)(AlarmReset.No)
                );
            }
            else
            {
                return new DomainEvent[0];
            }
        }

        private static Option<DomainEvent> CreateOptionOfDomainEvent(TimeSpan now, params IDataModelValue[] values) =>
            PropertiesChanged.Create(values, now);

        private static DomainEvent[] CreateDomainEvent(TimeSpan now, params IDataModelValue[] values) =>
            new DomainEvent[] {PropertiesChanged.Create(values, now)};

        private Func<AlarmState, IDataModelValue> CreateAlarmStateEvent(ModelFactory factory, TimeSpan now) =>
            alarmState =>
                (IDataModelValue) factory.CreateWithLog(_alarmUrn, alarmState, now).Value;

        private Func<AlarmReset, IDataModelValue>
            CreateAlarmReadyToResetEvent(ModelFactory factory, TimeSpan now) =>
            alarmReset =>
                (IDataModelValue) factory.CreateWithLog(_readyToResetUrn, alarmReset, now).Value;
    }
}