using System;
using System.Collections.Generic;
using ImpliciX.Harmony.Messages.Formatter;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using Alarm = ImpliciX.Harmony.Publishers.Alarm;

namespace ImpliciX.Harmony.Tests.Publishers
{
    [TestFixture]
    public class AlertsMessageTest
    {
        private static readonly Dictionary<string, (IDataModelValue, Option<(string, string, string, string)>,
            Func<PropertyUrn<AlarmState>, Option<string>>)> TestCases =
            new Dictionary<string, (IDataModelValue, Option<(string, string, string, string)>,
                Func<PropertyUrn<AlarmState>, Option<string>>)>
            {
                ["nominal_active"] = (HarmonyTestCommon.CreateAlarmProperty(new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc),
                    AlarmState.Active, alarms.C061), Option<(string, string, string, string)>.Some(
                    ("C061", "Active", "Abnormal", "2021-07-12T16:46:01.123000+00:00")
                ), urn => "C061"),
                ["nominal_inactive"] = (HarmonyTestCommon.CreateAlarmProperty(
                    new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc),
                    AlarmState.Inactive, alarms.C061), Option<(string, string, string, string)>.Some(
                    ("C061", "Inactive", "Normal", "2021-07-12T16:46:01.123000+00:00")
                ), urn => "C061"),
                ["not_an_alarm"] = (
                    Property<Literal>.Create(fake_model.fake_litteral, Literal.Create("Waza"), TimeSpan.Zero),
                    Option<(string, string, string, string)>.None(), urn => "C061"),
                ["unmapped_alarm"] = (
                    HarmonyTestCommon.CreateAlarmProperty(new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc),
                        AlarmState.Inactive, alarms.C061), Option<(string, string, string, string)>.None(),
                    urn => Option<string>.None())
            };

        [TestCase("nominal_active")]
        [TestCase("nominal_inactive")]
        [TestCase("not_an_alarm")]
        [TestCase("unmapped_alarm")]
        public void shoud_create_an_alert_from_a_data_model_value(string caseName)
        {
            var (property, expected, alarmNameFromUrn) = TestCases[caseName];
            var actual = Alarm.Create(property, alarmNameFromUrn);
            if (expected.IsNone)
                Check.That(actual.IsNone);
            else
            {
                Check.That(actual.IsSome).IsTrue();
                var alert = actual.GetValue();
                var (code, state, process, timeStamp) = expected.GetValue();
                Check.That(alert.Code).IsEqualTo(code);
                Check.That(alert.State).IsEqualTo(state);
                Check.That(alert.Process).IsEqualTo(process);
                Check.That(alert.Timestamp).IsEqualTo(timeStamp);
            }
        }

        [Test]
        public void should_serialize_a_message()
        {
            var alarmProperty = HarmonyTestCommon.CreateAlarmProperty(new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc),
                AlarmState.Active, alarms.C061);
            var currentTime = new DateTime(2021, 7, 12, 16, 46, 0, 0, DateTimeKind.Utc);
            var message = new AlarmsMessage(currentTime.Format(),
                Alarm.Create(alarmProperty, urn => "C061").GetValue());
            var json = new AlarmsMessageJson(new ContextStub("bhxxxxxxxx"), message);

            var expectedMessage =
                "{\"SerialNumber\":\"bhxxxxxxxx\",\"DateTime\":\"2021-07-12T16:46:00.000000+00:00\",\"Alerts\":[{\"Code\":\"C061\",\"State\":\"Active\",\"Process\":\"Abnormal\",\"Timestamp\":\"2021-07-12T16:46:01.123000+00:00\"}]}";

            Check.That(json.Format()).IsEqualTo(expectedMessage);
        }
    }
}