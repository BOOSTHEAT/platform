using System;
using ImpliciX.Data.Factory;
using ImpliciX.Harmony.States;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.States
{
    public class DisabledTest
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(fake_model).Assembly);
        }

        [Test]
        public void should_send_timeout_requests()
        {
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20));
            var harmonyModel = new HarmonyModuleDefinition() { EnableDelay = PropertyUrn<Duration>.Build("delay") };
            var sut = Runner.CreateWithSingleState(context, new Disabled(clock, harmonyModel.EnableDelay));
            var events = sut.Activate();
            var expected = NotifyOnTimeoutRequested.Create(harmonyModel.EnableDelay, clock.Now());
            Check.That(events[0]).Equals(expected);
        }

        [Test]
        public void should_send_enroll_with_dps()
        {
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20));
            var harmonyModel = new HarmonyModuleDefinition() { EnableDelay = PropertyUrn<Duration>.Build("delay") };
            var sut = Runner.CreateWithSingleState(context, new Disabled(clock, harmonyModel.EnableDelay));
            var events = sut.Handle(TimeoutOccured.Create(harmonyModel.EnableDelay, clock.Now(), Guid.Empty));
            Check.That(events[0].GetType()).Equals(typeof(Disabled.Enabled));
        }
    }
}