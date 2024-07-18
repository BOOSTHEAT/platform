using System;
using ImpliciX.Driver.Common.CommandAggregator;
using ImpliciX.Driver.Common.Tests.Buffer;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;
using fake_urn = ImpliciX.Driver.Common.Tests.Doubles.fake_urn;

namespace ImpliciX.Driver.Common.Tests.CommandAggregator
{
    [TestFixture]
    public class DomainEventsAggregatorTests
    {

        [Test]
        public void should_combine_two_similar_command_requested()
        {
            var c1 = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero);
            var c2 = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.5f).Value, TimeSpan.FromMilliseconds(1));

            var c3 = YoungestCommandAggregator.Combine(c1, c2);

            Check.That(c3.Value).IsEqualTo(c2);
        }

        [Test]
        public void should_not_combine_two_different_command_requested()
        {
            var c1 = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero);
            var c2 = CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromMilliseconds(1));

            var c3 = YoungestCommandAggregator.Combine(c1, c2);

            Check.That(c3.IsError).IsTrue();
        }

        [Test]
        public void should_not_combine_two_similar_command_requested_with_different_urns()
        {
            var c1 = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero);
            var c2 = CommandRequested.Create(fake_urn._throttle2, Percentage.FromFloat(0.4f).Value,
                TimeSpan.FromMilliseconds(1));

            var c3 = YoungestCommandAggregator.Combine(c1, c2);

            Check.That(c3.IsError).IsTrue();
        }
    }
}