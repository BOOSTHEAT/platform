using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Alarms.Tests
{
    [TestFixture]
    public class SensorAlarmsServiceTests
    {
        private readonly Dictionary<string, ((Urn, object)[], Option<AlarmState>)> _scenarii =
            new Dictionary<string, ((Urn, object)[], Option<AlarmState>)>
            {
                {
                    "case0", (new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Failure),
                    }, AlarmState.Inactive)
                },
                {
                    "case1", (new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Success),
                    }, Option<AlarmState>.None())
                },
                {
                    "case2", (new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Failure),
                        (fake.alarms_C061.settings.presence, Presence.Disabled)
                    }, Option<AlarmState>.None())
                },
                {
                    "case3", (new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Success),
                        (fake.alarms_C061.settings.presence, Presence.Disabled)
                    }, Option<AlarmState>.None())
                },
                {
                    "case4", (new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Success),
                        (fake.alarms_C061.settings.presence, Presence.Enabled)
                    }, AlarmState.Inactive)
                },
                {
                    "case5", (new (Urn, object)[]
                    {
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Failure),
                        (fake.alarms_C061.settings.presence, Presence.Enabled)
                    }, AlarmState.Inactive)
                },
                {
                    "case6", (new (Urn, object)[]
                    {
                        (fake.alarms_C061.settings.presence, Presence.Enabled),
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Success)
                    }, Option<AlarmState>.None())
                },
                {
                    "case7", (new (Urn, object)[]
                    {
                        (fake.alarms_C061.settings.presence, Presence.Enabled),
                        (fake.production_main_circuit_supply_temperature.status, MeasureStatus.Failure),
                    }, AlarmState.Active)
                },
            };

        [TestCase("case0")]
        [TestCase("case1")]
        [TestCase("case2")]
        [TestCase("case3")]
        [TestCase("case4")]
        [TestCase("case5")]
        [TestCase("case6")]
        [TestCase("case7")]
        public void should_return_sensor_alarm_event_on_sensor_status_or_alarm_presence_trigger(string scenarioName)
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            List<DomainEvent> result = null;
            var (inputs, expectedAlarmState) = _scenarii[scenarioName];
            foreach (var input in inputs)
            {
                var @event = EventPropertyChanged(new[] { input }, time.Now());
                result = alarmsService.HandlePropertiesChanged(@event).ToList();
            }

            if (expectedAlarmState.IsNone)
            {
                Check.That(result).IsEmpty();
            }
            else
            {
                var expected = new List<DomainEvent>()
                {
                    EventPropertyChanged(
                        new (Urn urn, object value)[] { (fake.alarms_C061.state, expectedAlarmState.GetValue()) },
                        time.Now())
                };
                Check.That(result).IsEqualTo(expected);
            }
        }
    }
}