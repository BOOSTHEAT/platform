using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Alarms
{
    public class AlarmAutoComputation : AlarmBase
    {
        private AlarmState _currentState;

        public AlarmAutoComputation(AlarmNode alarm)
        {
            _alarmUrn = alarm.state;
        }

        public Option<DomainEvent> ComputeAlarmState(IDataModelValue value, ModelFactory factory, TimeSpan now)
        {
            var nextState = value.ModelValue().Equals(Alert.Up) ? AlarmState.Active : AlarmState.Inactive;
            if (_currentState == nextState)
                return Option<DomainEvent>.None();
            else
            {
                _currentState = nextState;
                return PropertiesChanged.Create(new[]
                    {
                        (IDataModelValue)factory.CreateWithLog(_alarmUrn, _currentState, now).Value
                    },
                    now
                );
            }
        }
    }
}