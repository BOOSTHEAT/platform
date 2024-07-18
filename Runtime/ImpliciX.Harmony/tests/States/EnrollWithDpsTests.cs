using System;
using ImpliciX.Data.Factory;
using ImpliciX.Harmony.States;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Harmony.Tests.States
{
    [TestFixture]
    public class EnrollWithDpsTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(fake_model).Assembly);
        }

        [Test]
        public void should_return_EnrollmentFailed()
        {
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var fakeRegisterWithDps = new Func<string, DpsSettings, IotHubSettings, Result<string>>(
                (s, settings, arg3) => Result<string>.Create(new Error("", ""))
            );

            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20));
            var sut = Runner.CreateWithSingleState(context, new EnrollWithDps(clock, fakeRegisterWithDps));
            var events = sut.Activate();
            Check.That(events[0].GetType()).Equals(typeof(EnrollWithDps.EnrollmentFailed));
        }

        [Test]
        public void should_return_EnrollmentSuccess()
        {
            var dateTime = new DateTime(2021, 07, 16, 16, 53, 42, 503, DateTimeKind.Utc);
            var clock = new VirtualClock(dateTime);
            var fakeRegisterWithDps = new Func<string, DpsSettings, IotHubSettings, Result<string>>(
                (s, settings, arg3) => Result<string>.Create("success")
            );

            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20));
            var sut = Runner.CreateWithSingleState(context, new EnrollWithDps(clock, fakeRegisterWithDps));
            var events = sut.Activate();
            Check.That(events[0].GetType()).Equals(typeof(EnrollWithDps.EnrollmentSuccess));
        }
    }
}