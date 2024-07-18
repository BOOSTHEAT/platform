using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
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
    public class ManualAlarmsServiceTests
    {
        private static TestCaseData[] _testCases = new[]
        {
            new TestCaseData(new[] { Alert.Up }, Option<AlarmState>.Some(AlarmState.Active)),
            new TestCaseData(new[] { Alert.Up, Alert.Up }, Option<AlarmState>.None()),
            new TestCaseData(new[] { Alert.Up, Alert.Down }, Option<AlarmState>.Some(AlarmState.Inactive)),
            new TestCaseData(new[] { Alert.Up, Alert.Down, Alert.Down }, Option<AlarmState>.None()),
        };

        [TestCaseSource(nameof(_testCases))]
        public void should_return_alarm_property_changed_when_correct_propertyChanged_is_trigger(Alert[] source, Option<AlarmState> expectedState)
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            var result = Array.Empty<DomainEvent>();
            foreach (var alert in source)
            {
                var @event =
                    EventPropertyChanged(
                        new (Urn, object)[]
                            { (fake.production_main_circuit_supply_temperature_high_temperature.public_state, alert) },
                        time.Now());
                result = alarmsService.HandlePropertiesChanged(@event);
            }

            var expected = expectedState.Map(state =>
                new DomainEvent[]
                {
                    EventPropertyChanged(new (Urn urn, object value)[] { (fake.alarms_C029.state, state) },
                        time.Now())
                }
            );
            Check.That(result).IsEqualTo(expected.IsNone
                ? Array.Empty<DomainEvent>()
                : expected.GetValue());
        }

        [Test]
        public void should_return_alarmReset_to_Yes_event_when_correct_propertyChanged_is_trigger()
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;

            var @event =
                EventPropertyChanged(
                    new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature_high_temperature.ready_to_reset, Reset.Yes)
                    },
                    time.Now());
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);
            var result = alarmsService.HandlePropertiesChanged(@event).ToList();
            var expected = new List<DomainEvent>()
            {
                EventPropertyChanged(
                    new (Urn urn, object value)[] { (fake.alarms_C029.ready_to_reset, AlarmReset.Yes) },
                    time.Now())
            };
            Check.That(result).IsEqualTo(expected);
        }

        [Test]
        public void should_return_alarmReset_to_No_event_when_correct_propertyChanged_is_trigger()
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;

            var @event = EventPropertyChanged(new (Urn, object)[]
            {
                (fake.production_main_circuit_supply_temperature_high_temperature.ready_to_reset, Reset.No)
            }, time.Now());
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);
            var result = alarmsService.HandlePropertiesChanged(@event).ToList();
            var expected = new List<DomainEvent>()
            {
                EventPropertyChanged(new (Urn urn, object value)[] { (fake.alarms_C029.ready_to_reset, AlarmReset.No) },
                    time.Now())
            };
            Check.That(result).IsEqualTo(expected);
        }

        [Test]
        public void should_return_nothing_event_when_incorrect_propertyChanged_is_trigger()
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;

            var @event = EventPropertyChanged(new (Urn, object)[]
            {
                (fake.production_main_circuit_supply_temperature_high_temperature.settings.presence, Presence.Enabled)
            }, time.Now());
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);
            var result = alarmsService.HandlePropertiesChanged(@event).ToList();
            var expected = Array.Empty<DomainEvent>();
            Check.That(result).IsEqualTo(expected);
        }

        [Test]
        public void should_return_alarmReset_order_when_correct_commandRequest_is_trigger()
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;

            var @event = CommandRequested.Create(fake.alarms_C029._reset, new NoArg(), time.Now());
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);
            var result = alarmsService.HandleCommandRequested(@event).ToList();
            var expected = new[]
            {
                CommandRequested.Create(fake.production_main_circuit_supply_temperature_high_temperature._reset, new NoArg(),
                    time.Now())
            };
            Check.That(result).IsEqualTo(expected);
        }
    }
}