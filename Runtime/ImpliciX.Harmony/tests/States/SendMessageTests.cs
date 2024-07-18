using System;
using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Harmony.Infrastructure;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Messages.Formatter;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Harmony.States;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using NUnit.Framework;
using Alarm = ImpliciX.Harmony.Publishers.Alarm;
using Metrics = ImpliciX.Harmony.Publishers.Metrics;

namespace ImpliciX.Harmony.Tests.States
{
    [TestFixture]
    public class SendMessageTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(fake_model).Assembly);
        }

        private static readonly Dictionary<string, (bool, Func<Type[]>)> TestCases =
            new Dictionary<string, (bool, Func<Type[]>)>
            {
                ["send-success"] = (true, () => Type.EmptyTypes),
                ["send-failure"] = (false, () => new[]
                {
                    typeof(ConnectToIotHub.ConnectionFailed)
                })
            };

        [TestCase("send-success")]
        [TestCase("send-failure")]
        public void send_discovery_messages_test(string caseName)
        {
            var (sendResult, expectedTypes) = TestCases[caseName];
            var azureIoTAdapter = new Mock<IAzureIoTHubAdapter>();
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20))
            {
                SerialNumber = "42",
                AzureIoTHubAdapter = azureIoTAdapter.Object,
                ReleaseVersion = "1.2.3.42",
                UserTimeZone = "Europe__Paris",
                DeviceId = "bhyolo"
            };
            azureIoTAdapter.Setup(adapter => adapter.SendMessage(It.IsAny<IHarmonyMessage>(), context)).Returns(sendResult);
            var sut = Runner.CreateWithSingleState(context,
                new SendMessages(clock, new Queue<IHarmonyMessage>(), context));
            var expected = SendMessages.CreateDiscoveryMessage(context, clock.Now());
            var domainEvents = sut.Activate();
            Check.That(domainEvents.GetTypes()).IsEqualTo(expectedTypes());
            azureIoTAdapter.Verify(a => a.SendMessage(expected, context), Times.Once);
        }

        [TestCase("send-success")]
        [TestCase("send-failure")]
        public void should_send_alert_messages(string caseName)
        {
            var (sendResult, expectedTypes) = TestCases[caseName];
            var azureIoTAdapter = new Mock<IAzureIoTHubAdapter>();
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20))
            {
                SerialNumber = "bhyolo",
                AzureIoTHubAdapter = azureIoTAdapter.Object
            };
            azureIoTAdapter.Setup(adapter => adapter.SendMessage(It.IsAny<IHarmonyMessage>(), context)).Returns(sendResult);
            var queue = new Queue<IHarmonyMessage>();
            var alarmStates = new Alarms(urn => "C061", queue);
            var alarmPropertyChanged =
                HarmonyTestCommon.CreateAlarmPropertyChanged(dateTime, AlarmState.Active, alarms.C061);
            alarmStates.Handles(alarmPropertyChanged);
            var sut = Runner.CreateWithSingleState(context, new SendMessages(clock, queue, context));
            var propertyChanged = EventsHelper.EventSystemTicked(1000, TimeSpan.Zero);
            var domainEvents = sut.Handle(propertyChanged);
            var expected = new AlarmsMessage(dateTime.Format(), new Alarm()
            {
                Code = "C061",
                State = "Active",
                Process = "Abnormal",
                Timestamp = "2021-07-16T16:53:42.503000+00:00"
            });
            Check.That(domainEvents.GetTypes()).IsEqualTo(expectedTypes());
            azureIoTAdapter.Verify(a => a.SendMessage(expected, context), Times.Once);
        }

        [TestCase("send-success")]
        [TestCase("send-failure")]
        public void should_send_analytics(string caseName)
        {
            var (sendResult, expectedTypes) = TestCases[caseName];
            var azureIoTAdapter = new Mock<IAzureIoTHubAdapter>();
            var sampleStartTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var sampleEndTime = new DateTime(2021, 07, 16, 17, 00, 00, 000, DateTimeKind.Utc);
            var busPublicationDateTime = new DateTime(2021, 07, 16, 17, 00, 00, 421, DateTimeKind.Utc);
            var harmonyPublicationDateTime = new DateTime(2021, 07, 16, 17, 00, 01, 322, DateTimeKind.Utc);
            var clock = new VirtualClock(harmonyPublicationDateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20))
            {
                SerialNumber = "bhyolo",
                AzureIoTHubAdapter = azureIoTAdapter.Object
            };
            azureIoTAdapter.Setup(adapter => adapter.SendMessage(It.IsAny<IHarmonyMessage>(), context)).Returns(sendResult);
            var harmonyQueue = new Queue<IHarmonyMessage>();
            var metrics = new Metrics(harmonyQueue);

            var propertiesChanged = PropertiesChanged.Create(new IDataModelValue[]
                {
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "foo", "occurrence"),
                        new MetricValue(42, TimeSpan.FromTicks(sampleStartTime.Ticks),
                            TimeSpan.FromTicks(sampleEndTime.Ticks)), TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "foo", "duration"),
                        new MetricValue(19, TimeSpan.FromTicks(sampleStartTime.Ticks),
                            TimeSpan.FromTicks(sampleEndTime.Ticks)), TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "bar", "occurrence"),
                        new MetricValue(13, TimeSpan.FromTicks(sampleStartTime.Ticks),
                            TimeSpan.FromTicks(sampleEndTime.Ticks)), TimeSpan.FromTicks(busPublicationDateTime.Ticks)),
                    Property<MetricValue>.Create(MetricUrn.Build(test_model.test_counters_a, "bar", "duration"),
                        new MetricValue(37, TimeSpan.FromTicks(sampleStartTime.Ticks),
                            TimeSpan.FromTicks(sampleEndTime.Ticks)), TimeSpan.FromTicks(busPublicationDateTime.Ticks))
                }
                , TimeSpan.FromTicks(busPublicationDateTime.Ticks));


            metrics.Handles(propertiesChanged);

            var sut = Runner.CreateWithSingleState(context, new SendMessages(clock, harmonyQueue, context));
            var propertyChanged = EventsHelper.EventSystemTicked(1000, TimeSpan.Zero);
            var result = sut.Handle(propertyChanged);

            var expected =
                @"{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-16T17:00:00.421000+00:00"",""SampleStartTime"":""2021-07-16T16:53:42.503000+00:00"",""SampleEndTime"":""2021-07-16T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:foo:occurrence"",""Value"":42}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-16T17:00:00.421000+00:00"",""SampleStartTime"":""2021-07-16T16:53:42.503000+00:00"",""SampleEndTime"":""2021-07-16T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:foo:duration"",""Value"":19}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-16T17:00:00.421000+00:00"",""SampleStartTime"":""2021-07-16T16:53:42.503000+00:00"",""SampleEndTime"":""2021-07-16T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:bar:occurrence"",""Value"":13}
{""Version"":1,""SerialNumber"":""bhyolo"",""PublicationDateTime"":""2021-07-16T17:00:00.421000+00:00"",""SampleStartTime"":""2021-07-16T16:53:42.503000+00:00"",""SampleEndTime"":""2021-07-16T17:00:00.000000+00:00"",""Urn"":""test_model:test_counters_a:bar:duration"",""Value"":37}
";

            Check.That(result.GetTypes()).IsEqualTo(expectedTypes());
            azureIoTAdapter.Verify(a => a.SendMessage(It.Is<IHarmonyMessage>(message => message.Format(context) == expected), context),
                Times.Once);
        }
    }
}