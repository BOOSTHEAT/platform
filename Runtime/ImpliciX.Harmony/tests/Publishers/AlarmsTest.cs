using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.Publishers
{
    [TestFixture]
    public class AlarmsTest
    {
        [Test]
        public void should_enqueue_alerts()
        {
            var virtualClock = new VirtualClock(new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc));
            var queue = new Queue<IHarmonyMessage>();
            var alarmStates = new Alarms(urn =>
            {
                if (urn == test_model.C998.state) return nameof(test_model.C998);
                return urn == test_model.C999.state
                    ? nameof(test_model.C999)
                    : string.Empty;
            }, queue);

            var alarmStateProperty1 =
                Property<AlarmState>.Create(test_model.C998.state, AlarmState.Active, virtualClock.Now());
            var alarmStateProperty2 =
                Property<AlarmState>.Create(test_model.C999.state, AlarmState.Inactive, virtualClock.Now());

            var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
            {
                Property<Literal>.Create(test_model.dummy, Literal.Create("bar"), virtualClock.Now()),
                alarmStateProperty1,
                alarmStateProperty2
            }, virtualClock.Now());

            alarmStates.Handles(propertiesChanged);

            var expected =
                @"{""SerialNumber"":""bhyolo"",""DateTime"":""2021-07-12T16:46:01.123000+00:00"",""Alerts"":[{""Code"":""C998"",""State"":""Active"",""Process"":""Abnormal"",""Timestamp"":""2021-07-12T16:46:01.123000+00:00""},{""Code"":""C999"",""State"":""Inactive"",""Process"":""Normal"",""Timestamp"":""2021-07-12T16:46:01.123000+00:00""}]}";

            Check.That(queue.Count).IsEqualTo(1);
            Check.That(queue.Any(c => Equals(c.Format(new ContextStub("bhyolo")), expected))).IsTrue();
        }

        [Test]
        public void should_not_enqueue_when_no_alerts()
        {
            var virtualClock = new VirtualClock(new DateTime(2021, 07, 12, 16, 46, 01, 123, DateTimeKind.Utc));
            var queue = new Queue<IHarmonyMessage>();
            var alarmStates = new Alarms(urn => string.Empty, queue);
            var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
            {
                Property<Literal>.Create(test_model.dummy, Literal.Create("bar"), virtualClock.Now())
            }, TimeSpan.Zero);

            alarmStates.Handles(propertiesChanged);

            Check.That(queue.Count).IsEqualTo(0);
        }
    }
}