using System;
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
    public class AutoAlarmsServiceTests
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
                            { (fake.whirlpool_activation.public_state, alert) },
                        time.Now());
                result = alarmsService.HandlePropertiesChanged(@event);
            }

            var expected = expectedState.Map(state =>
                new DomainEvent[]
                {
                    EventPropertyChanged(new (Urn urn, object value)[] { (fake.alarms_C666.state, state) },
                        time.Now())
                }
            );
            Check.That(result).IsEqualTo(expected.IsNone
                ? Array.Empty<DomainEvent>()
                : expected.GetValue());
        }
    }
}