using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Alarms
{
    public class AlarmDeviceComputation : AlarmBase
    {
        private int _errorCount;
        private AlarmState _currentAlarmState;
        private readonly Func<CommunicationStatus, int, (AlarmState, int)> _transition;

        public AlarmDeviceComputation(int maxErrors, PropertyUrn<AlarmState> alarmUrn)
        {
            _currentAlarmState = AlarmState.Inactive;
            _transition = (newStatus, errorCount) =>
                newStatus switch
                {
                    CommunicationStatus.Healthy => (AlarmState.Inactive, 0),
                    CommunicationStatus.Error when maxErrors <= 1 => (AlarmState.Active, maxErrors),
                    CommunicationStatus.Error when errorCount < maxErrors => (AlarmState.Inactive, errorCount + 1),
                    CommunicationStatus.Error when errorCount >= maxErrors => (AlarmState.Active, maxErrors),
                    CommunicationStatus.Fatal => (AlarmState.Active, maxErrors),
                    _ => throw new ArgumentOutOfRangeException()
                };
            _alarmUrn = alarmUrn;
        }

        public Func<SlaveCommunicationOccured, ModelFactory, TimeSpan, DomainEvent[]> OnSlaveCommunicationOccured =>
            (slaveCommunicationOccured, modelFactory, now) =>
            {
                var newStatus = slaveCommunicationOccured.CommunicationStatus;
                var (newAlarmState, errorCount) = _transition(newStatus, _errorCount);
                _errorCount = errorCount;
                switch (_currentAlarmState, newAlarmState)
                {
                    case (AlarmState.Active, AlarmState.Active):
                    case (AlarmState.Inactive, AlarmState.Inactive):
                        return Array.Empty<DomainEvent>();
                    case (_, _):
                        _currentAlarmState = newAlarmState;
                        return CreateDomainEvent(modelFactory, now)(newAlarmState);
                }
            };

        private Func<AlarmState, DomainEvent[]> CreateDomainEvent(ModelFactory modelFactory, TimeSpan now) =>
            alarmState => new DomainEvent[]
            {
                PropertiesChanged.Create(new[]
                    {
                        (IDataModelValue) modelFactory.CreateWithLog(_alarmUrn, alarmState, now).Value
                    },
                    now
                )
            };
    }
}