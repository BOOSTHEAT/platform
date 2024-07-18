using System;
using ImpliciX.Data.Factory;
using ImpliciX.Harmony.Infrastructure;
using ImpliciX.Harmony.States;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.States
{
    [TestFixture]
    public class WaitBeforeRetryTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(fake_model).Assembly);
        }

        [Test]
        public void should_send_timeout_requests()
        {
            var azureIoTAdapter = new Mock<IAzureIoTHubAdapter>();
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20))
            {
                AzureIoTHubAdapter = azureIoTAdapter.Object,
            };
            var harmonyModel = new HarmonyModuleDefinition() { RetryDelay = PropertyUrn<Duration>.Build("delay") };
            var sut = Runner.CreateWithSingleState(context,
                new WaitBeforeRetryTest(clock, harmonyModel.RetryDelay,
                    new TestRetryContext(3)));
            var events = sut.Activate();
            var expected = NotifyOnTimeoutRequested.Create(harmonyModel.RetryDelay, clock.Now());
            Check.That(events[0]).Equals(expected);
            azureIoTAdapter.Verify(a => a.Dispose(), Times.Once);
        }

        [Test]
        public void should_send_connection_disabled()
        {
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20));
            var harmonyModel = new HarmonyModuleDefinition() { RetryDelay = PropertyUrn<Duration>.Build("delay") };
            var sut = Runner.CreateWithSingleState(context,
                new WaitBeforeRetryTest(clock, harmonyModel.RetryDelay,
                    new TestRetryContext(3)));
            var events = sut.Handle(TimeoutOccured.Create(harmonyModel.RetryDelay, clock.Now(), Guid.Empty));
            Check.That(events[0].GetType()).Equals(typeof(WaitBeforeRetry.ConnectionDisabled));
        }
    }

    public class WaitBeforeRetryTest : WaitBeforeRetry
    {
        public WaitBeforeRetryTest(IClock clock, PropertyUrn<Duration> retryDelayUrn,
            HarmonyRetryContext retryContext) : base(nameof(WaitBeforeRetryTest), clock, retryDelayUrn, retryContext)
        {
        }
    }

    public class TestRetryContext : HarmonyRetryContext
    {
        public TestRetryContext(int retries) : base(retries)
        {
        }
    }
}