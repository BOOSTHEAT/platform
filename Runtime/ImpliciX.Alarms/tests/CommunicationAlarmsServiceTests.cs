using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Alarms.Tests
{
    [TestFixture]
    public class CommunicationAlarmsServiceTests
    {
        private readonly Dictionary<string, (DeviceNode device, PropertyUrn<AlarmState> alarmStateUrn)>
            _deviceAlarmMapping =
                new Dictionary<string, (DeviceNode device, PropertyUrn<AlarmState> alarmStateUrn)>
                {
                    { "iu", (fake.devices_bh20_iu, fake.alarms_C063.state) },
                    { "eu", (fake.devices_bh20_eu, fake.alarms_C064.state) },
                    { "heat_pump", (fake.devices_bh20_heat_pump, fake.alarms_C065.state) }
                };

        [TestCase("iu")]
        [TestCase("eu")]
        [TestCase("heat_pump")]
        public void should_return_active_alarm_when_fatal_communication_event_is_trigger(string deviceKey)
        {
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            var @event =
                SlaveCommunicationOccured.CreateFatal(_deviceAlarmMapping[deviceKey].device,
                    TimeSpan.Zero, new CommunicationDetails(0, 1));
            var result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();

            var expected = new List<DomainEvent>()
            {
                EventPropertyChanged(new (Urn urn, object value)[]
                    {
                        (_deviceAlarmMapping[deviceKey].alarmStateUrn, AlarmState.Active)
                    },
                    time.Now())
            };
            Check.That(result).IsEqualTo(expected);
        }

        [TestCase("iu")]
        [TestCase("eu")]
        [TestCase("heat_pump")]
        public void should_return_no_domain_events_when_one_communication_error_event_is_trigger(string deviceKey)
        {
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            var @event =
                SlaveCommunicationOccured.CreateError(_deviceAlarmMapping[deviceKey].device, TimeSpan.Zero,
                    new CommunicationDetails(0, 1));
            var result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();
            Check.That(result).IsEmpty();
        }

        [TestCase("iu", 6)]
        [TestCase("eu", 4)]
        [TestCase("heat_pump", 3)]
        public void
            should_return_active_alarm_when_communication_error_event_are_reached_consecutive_error_before_failure(
                string deviceKey, int consecutiveSlaveCommunicationErrorsBeforeFailure)
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            var @event =
                SlaveCommunicationOccured.CreateError(_deviceAlarmMapping[deviceKey].device,
                    TimeSpan.Zero, new CommunicationDetails(0, 1));

            var expectedActive = new List<DomainEvent>()
            {
                EventPropertyChanged(new (Urn urn, object value)[]
                    {
                        (_deviceAlarmMapping[deviceKey].alarmStateUrn, AlarmState.Active)
                    },
                    time.Now())
            };

            List<DomainEvent> result;
            for (var i = 0; i < consecutiveSlaveCommunicationErrorsBeforeFailure; i++)
            {
                result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();
                Check.That(result).IsEqualTo(Array.Empty<DomainEvent>());
            }

            result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();
            Check.That(result).IsEqualTo(expectedActive);
        }

        [TestCase("iu", 6)]
        [TestCase("eu", 4)]
        [TestCase("heat_pump", 3)]
        public void
            should_return_active_alarm_when_communication_error_event_are_reached_consecutive_error_before_failure_in_override(
                string deviceKey, int consecutiveSlaveCommunicationErrorsBeforeFailure)
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations,
                        Helpers.CreateSettings(2, (fake.devices_bh20_iu.Urn, 6), (fake.devices_bh20_eu.Urn, 4), (fake.devices_bh20_heat_pump.Urn, 3))),
                    time,
                    factory);

            var @event =
                SlaveCommunicationOccured.CreateError(_deviceAlarmMapping[deviceKey].device,
                    TimeSpan.Zero, new CommunicationDetails(0, 1));

            var expectedActive = new List<DomainEvent>()
            {
                EventPropertyChanged(new (Urn urn, object value)[]
                    {
                        (_deviceAlarmMapping[deviceKey].alarmStateUrn, AlarmState.Active)
                    },
                    time.Now())
            };

            List<DomainEvent> result;
            for (var i = 0; i < consecutiveSlaveCommunicationErrorsBeforeFailure; i++)
            {
                result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();
                Check.That(result).IsEqualTo(Array.Empty<DomainEvent>());
            }

            result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();
            Check.That(result).IsEqualTo(expectedActive);
        }

        [TestCase("iu", 0)]
        [TestCase("eu", 1)]
        [TestCase("heat_pump", 0)]
        public void should_always_return_active_alarm_when_consecutive_errors_before_failure_is_set_to_zero_or_one(
            string deviceKey, int consecutiveSlaveCommunicationErrorsBeforeFailure)
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time, factory);

            var @event = SlaveCommunicationOccured.CreateError(_deviceAlarmMapping[deviceKey].device, TimeSpan.Zero,
                new CommunicationDetails(0, 1));

            var expectedActive = new List<DomainEvent>()
            {
                EventPropertyChanged(new (Urn urn, object value)[]
                    {
                        (_deviceAlarmMapping[deviceKey].alarmStateUrn, AlarmState.Active)
                    },
                    time.Now())
            };

            var result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();
            Check.That(result).IsEqualTo(expectedActive);
        }


        [TestCase("iu")]
        [TestCase("eu")]
        [TestCase("heat_pump")]
        public void should_return_no_domain_events_when_healthy_communication_event_is_trigger(string deviceKey)
        {
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            var @event =
                SlaveCommunicationOccured.CreateHealthy(_deviceAlarmMapping[deviceKey].device,
                    TimeSpan.Zero, new CommunicationDetails(1, 0));
            var result = alarmsService.HandleSlaveCommunicationOccured(@event).ToList();

            Check.That(result).IsEmpty();
        }
    }
}