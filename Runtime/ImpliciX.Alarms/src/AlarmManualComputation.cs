using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Alarms
{
    public class AlarmManualComputation : AlarmBase
    {
        private readonly PropertyUrn<AlarmReset> _readyToResetUrn;
        private readonly Urn _resetCommandUrn;
        private AlarmState _currentState;

        public AlarmManualComputation(AlarmNode alarm, Urn resetCommandUrn)
        {
            _readyToResetUrn = alarm.ready_to_reset;
            _alarmUrn = alarm.state;
            _resetCommandUrn = resetCommandUrn;
            _currentState = AlarmState.Inactive;
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
                        (IDataModelValue) factory.CreateWithLog(_alarmUrn, _currentState, now).Value
                    },
                    now
                );
            }
        }

        public Option<DomainEvent> ComputeAlarmReset(IDataModelValue value, ModelFactory factory, TimeSpan now) =>
            PropertiesChanged.Create(new[]
                {
                    (IDataModelValue) factory
                        .CreateWithLog(_readyToResetUrn,
                            value.ModelValue().Equals(Reset.Yes) ? AlarmReset.Yes : AlarmReset.No, now)
                        .Value
                },
                now
            );

        public DomainEvent[] OnResetCommand(ModelFactory _, TimeSpan now) =>
            new DomainEvent[]
            {
                CommandRequested.Create(CommandUrn<NoArg>.Build(_resetCommandUrn), new NoArg(), now)
            };
    }
}