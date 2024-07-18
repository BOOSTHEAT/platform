using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using ImpliciX.ThingsBoard.Infrastructure;
using ImpliciX.ThingsBoard.States;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ThingsBoard.Tests.States
{
    [TestFixture]
    public class WaitBeforeRetryTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(test_model).Assembly);
        }

        [Test]
        public void should_send_timeout_requests()
        {
            var azureIoTAdapter = new Mock<IMqttAdapter>();
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", new ThingsBoardSettings { GlobalRetries = 3 })
            {
                Adapter = azureIoTAdapter.Object,
            };
            var harmonyModel = new ThingsBoardModuleDefinition { RetryDelay = PropertyUrn<Duration>.Build("delay") };
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
            var context = new Context("the-app", new ThingsBoardSettings { GlobalRetries = 3 });
            var harmonyModel = new ThingsBoardModuleDefinition { RetryDelay = PropertyUrn<Duration>.Build("delay") };
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
            RetryContext retryContext) : base(nameof(WaitBeforeRetryTest), clock, retryDelayUrn, retryContext)
        {
        }
    }

    public class TestRetryContext : RetryContext
    {
        public TestRetryContext(int retries) : base(retries)
        {
        }
    }
}