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
    public class PumpAlarmsServiceTests
    {
        private readonly Dictionary<string, ((Urn, object)[], Option<(Urn, object)[]>)>
            _scenariiFunctionalStateTrigger =
                new Dictionary<string, ((Urn, object)[], Option<(Urn, object)[]>)>
                {
                    {
                        "case0", (new (Urn, object)[]
                        {
                            (fake.alarms_C076.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.running),
                        }, Option<(Urn, object)[]>.None())
                    },
                    {
                        "case1", (new (Urn, object)[]
                        {
                            (fake.alarms_C075.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked),
                        }, new (Urn, object)[] { (fake.alarms_C075.state, AlarmState.Active) })
                    },
                    {
                        "case2", (new (Urn, object)[]
                        {
                            (fake.alarms_C076.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                        }, Option<(Urn, object)[]>.None())
                    },
                    {
                        "case3", (new (Urn, object)[]
                        {
                            (fake.alarms_C075.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.running),
                        }, new (Urn, object)[] { (fake.alarms_C075.ready_to_reset, AlarmReset.Yes) })
                    },
                    {
                        "case4", (new (Urn, object)[]
                        {
                            (fake.alarms_C076.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.standby_stop),
                        }, new (Urn, object)[] { (fake.alarms_C076.ready_to_reset, AlarmReset.Yes) })
                    },
                    {
                        "case5", (new (Urn, object)[]
                        {
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.standby_stop),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                        }, Option<(Urn, object)[]>.None())
                    },
                };

        [TestCase("case0")]
        [TestCase("case1")]
        [TestCase("case2")]
        [TestCase("case3")]
        [TestCase("case4")]
        [TestCase("case5")]
        public void should_return_domain_events_on_functional_state_trigger(string scenarioName)
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
            var (inputs, expectations) = _scenariiFunctionalStateTrigger[scenarioName];
            foreach (var input in inputs)
            {
                var @event = EventPropertyChanged(new[] { input }, time.Now());
                result = alarmsService.HandlePropertiesChanged(@event).ToList();
            }

            if (expectations.IsNone)
            {
                Check.That(result).IsEmpty();
            }
            else
            {
                var expected = new List<DomainEvent>()
                {
                    EventPropertyChanged(expectations.GetValue(), time.Now())
                };
                Check.That(result).IsEqualTo(expected);
            }
        }

        private readonly Dictionary<string, ((Urn, object)[], Option<(Urn, object)[]>, Urn)> _scenariiResetTrigger =
            new Dictionary<string, ((Urn, object)[], Option<(Urn, object)[]>, Urn)>
            {
                {
                    "case1", (new (Urn, object)[]
                    {
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                    }, Option<(Urn, object)[]>.None(), fake.alarms_C076._reset)
                },
                {
                    "case2", (new (Urn, object)[]
                        {
                            (fake.alarms_C076.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.standby_stop),
                        },
                        new (Urn, object)[]
                        {
                            (fake.alarms_C076.state, AlarmState.Inactive),
                            (fake.alarms_C076.ready_to_reset, AlarmReset.No)
                        }, fake.alarms_C076._reset)
                },
                {
                    "case3", (new (Urn, object)[]
                    {
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.standby_stop),
                        (fake.alarms_C076.settings.presence, Presence.Disabled),
                    }, Option<(Urn, object)[]>.None(), fake.alarms_C076._reset)
                },
                {
                    "case4", (new (Urn, object)[]
                        {
                            (fake.alarms_C075.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.standby_stop),
                        },
                        new (Urn, object)[]
                        {
                            (fake.alarms_C075.state, AlarmState.Inactive),
                            (fake.alarms_C075.ready_to_reset, AlarmReset.No)
                        }, fake.alarms_C075._reset)
                },
            };


        [TestCase("case1")]
        [TestCase("case2")]
        [TestCase("case3")]
        [TestCase("case4")]
        public void should_return_domain_events_on_reset_trigger(string scenarioName)
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
            var (inputs, expectations, resetUrn) = _scenariiResetTrigger[scenarioName];
            foreach (var input in inputs)
            {
                var @event = EventPropertyChanged(new[] { input }, time.Now());
                result = alarmsService.HandlePropertiesChanged(@event).ToList();
            }

            var commandRequested = EventCommandRequested(resetUrn, new NoArg(), time.Now());
            result = alarmsService.HandleCommandRequested(commandRequested).ToList();

            if (expectations.IsNone)
            {
                Check.That(result).IsEmpty();
            }
            else
            {
                var expected = new List<DomainEvent>()
                {
                    EventPropertyChanged(expectations.GetValue(), time.Now())
                };
                Check.That(result).IsEqualTo(expected);
            }
        }

        private readonly Dictionary<string, ((Urn, object)[], Option<(Urn, object)[]>)> _scenariiDisableTrigger =
            new Dictionary<string, ((Urn, object)[], Option<(Urn, object)[]>)>
            {
                {
                    "case1", (new (Urn, object)[]
                    {
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                        (fake.alarms_C076.settings.presence, Presence.Disabled),
                    }, new (Urn, object)[] { (fake.alarms_C076.state, AlarmState.Inactive) })
                },
                {
                    "case2", (new (Urn, object)[]
                    {
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                    }, Option<(Urn, object)[]>.None())
                },
                {
                    "case3", (new (Urn, object)[]
                    {
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                    }, new (Urn, object)[] { (fake.alarms_C076.state, AlarmState.Inactive) })
                },
                {
                    "case4", (new (Urn, object)[]
                    {
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Undervoltage_stop),
                    }, new (Urn, object)[] { (fake.alarms_C076.state, AlarmState.Active) })
                },
                {
                    "case5", (new (Urn, object)[]
                    {
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                    }, new (Urn, object)[] { (fake.alarms_C076.state, AlarmState.Inactive) })
                },
                {
                    "case6", (new (Urn, object)[]
                    {
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                        (fake.alarms_C076.settings.presence, Presence.Enabled),
                    }, Option<(Urn, object)[]>.None())
                },
                {
                    "case7", (new (Urn, object)[]
                    {
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked),
                        (fake.alarms_C075.settings.presence, Presence.Enabled),
                        (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked),
                    }, new (Urn, object)[] { (fake.alarms_C075.state, AlarmState.Active) })
                },
                {
                    "case8", (new (Urn, object)[]
                        {
                            (fake.alarms_C075.settings.presence, Presence.Enabled),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked),
                            (fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.standby_stop),
                            (fake.alarms_C075.settings.presence, Presence.Disabled),
                        },
                        new (Urn, object)[]
                        {
                            (fake.alarms_C075.state, AlarmState.Inactive),
                            (fake.alarms_C075.ready_to_reset, AlarmReset.No)
                        })
                },
            };

        [TestCase("case1")]
        [TestCase("case2")]
        [TestCase("case3")]
        [TestCase("case4")]
        [TestCase("case5")]
        [TestCase("case6")]
        [TestCase("case7")]
        [TestCase("case8")]
        public void should_return_domain_events_on_disable_trigger(string scenarioName)
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
            var (inputs, expectations) = _scenariiDisableTrigger[scenarioName];
            foreach (var input in inputs)
            {
                var @event = EventPropertyChanged(new[] { input }, time.Now());
                result = alarmsService.HandlePropertiesChanged(@event).ToList();
            }

            if (expectations.IsNone)
            {
                Check.That(result).IsEmpty();
            }
            else
            {
                var expected = new List<DomainEvent>()
                {
                    EventPropertyChanged(expectations.GetValue(), time.Now())
                };
                Check.That(result).IsEqualTo(expected);
            }
        }

        [Test]
        public void should_return_domain_events_on_activation()
        {
            var factory = Setup.TheModelFactory;
            var time = new StubClock();
            var consecutiveSlaveCommunicationErrorsBeforeFailure = 2;
            var alarmsService =
                new AlarmsService(
                    new AlarmsDefinitions(AllAlarms.Declarations, Helpers.CreateSettings(consecutiveSlaveCommunicationErrorsBeforeFailure)),
                    time,
                    factory);

            var result = alarmsService.ActivateAlarms;
            var expected = EventPropertyChanged(new (Urn, object)[]
            {
                (fake.alarms_C075.state, AlarmState.Inactive),
                (fake.alarms_C076.state, AlarmState.Inactive),
                (fake.alarms_C077.state, AlarmState.Inactive)
            }, time.Now());
            Check.That(result.ModelValues).Contains(expected.ModelValues);
        }
    }
}