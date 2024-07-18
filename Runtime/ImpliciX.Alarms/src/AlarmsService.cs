using System;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.Alarms
{
    public class AlarmsService
    {
        private readonly AlarmsDefinitions _alarmsDefinitions;
        private readonly IClock _clock;
        private readonly ModelFactory _modelFactory;

        public AlarmsService(AlarmsDefinitions alarmsDefinitions, IClock clock, ModelFactory modelFactory)
        {
            _alarmsDefinitions = alarmsDefinitions;
            _clock = clock;
            _modelFactory = modelFactory;
        }

        public PropertiesChanged ActivateAlarms =>
            PropertiesChanged.Create(
                _alarmsDefinitions.AlarmActivationComputation.Select(f => f(_modelFactory, _clock.Now())),
                _clock.Now());

        public Func<CommandRequested, bool> CanHandleCommandRequested =>
            commandRequested =>
                _alarmsDefinitions.AlarmResetCommand.ContainsKey(commandRequested.Urn);

        public DomainEventHandler<CommandRequested> HandleCommandRequested =>
            commandRequested =>
                _alarmsDefinitions.AlarmResetCommand[commandRequested.Urn]
                    .SelectMany(f => f(_modelFactory, _clock.Now())).ToArray();

        public Func<PropertiesChanged, bool> CanHandlePropertiesChanged =>
            propertiesChanged =>
                propertiesChanged.PropertiesUrns.Any(c => _alarmsDefinitions.AlarmStateComputation.ContainsKey(c));

        public DomainEventHandler<PropertiesChanged> HandlePropertiesChanged =>
            propertiesChanged =>
                propertiesChanged.ModelValues
                    .Where(c => _alarmsDefinitions.AlarmStateComputation.ContainsKey(c.Urn))
                    .SelectMany(c =>
                        _alarmsDefinitions.AlarmStateComputation[c.Urn]
                            .Select(f => f(c, _modelFactory, _clock.Now())))
                    .Where(c => c.IsSome)
                    .Select(c => c.GetValue())
                    .ToArray();

        public Func<SlaveCommunicationOccured, bool> CanHandleSlaveCommunicationOccured =>
            communicationOccured =>
                _alarmsDefinitions.AlarmDeviceComputation.ContainsKey(communicationOccured.DeviceNode.Urn);

        public DomainEventHandler<SlaveCommunicationOccured> HandleSlaveCommunicationOccured =>
            slaveCommunicationOccured =>
                _alarmsDefinitions.AlarmDeviceComputation[slaveCommunicationOccured.DeviceNode.Urn]
                    .SelectMany(f => f(slaveCommunicationOccured, _modelFactory, _clock.Now())).ToArray();
    }
}