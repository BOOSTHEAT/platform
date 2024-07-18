using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.Buffer;
using ImpliciX.Driver.Common.CommandAggregator;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Driver.Common.Tests.Buffer
{
    [TestFixture]
    public class CommandRequestedBufferTests
    {
        [Test]
        public void when_commandRequested_and_systemTicked_are_received_should_return_domain_events()
        {
            var sut = CreateSUT();
            var cr = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.1f).Value, TimeSpan.Zero);
            sut.ReceivedCommandRequested(cr);

            var result = sut.ReleaseCommandRequested();

            Check.That(result).Not.IsEmpty();
        }

        [Test]
        public void when_systemTicked_is_received_should_return_no_domain_events()
        {
            var sut = CreateSUT();

            var result = sut.ReleaseCommandRequested();

            Check.That(result).IsEmpty();
        }

        [Test]
        public void when_commandRequested_and_systemTicked_are_received_should_return_the_received_event()
        {
            var sut = CreateSUT();
            var cr = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.1f).Value, TimeSpan.Zero);
            sut.ReceivedCommandRequested(cr);

            var result = sut.ReleaseCommandRequested();

            Check.That(result).IsEqualTo(new DomainEvent[] {cr});
        }

        [Test]
        public void
            when_aggregatable_commandRequested_is_received_and_systemTicked_is_received_twice_it_should_return_no_domain_events()
        {
            var sut = CreateSUT();
            var cr = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.1f).Value, TimeSpan.Zero);
            sut.ReceivedCommandRequested(cr);
            sut.ReleaseCommandRequested();

            var result = sut.ReleaseCommandRequested();

            Check.That(result).IsEmpty();
        }

        [Test]
        public void
            when_not_aggregatable_commandRequested_is_received_and_systemTicked_is_received_twice_it_should_return_no_domain_events()
        {
            var sut = CreateSUT();
            var cr = CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.Zero);
            sut.ReceivedCommandRequested(cr);
            sut.ReleaseCommandRequested();

            var result = sut.ReleaseCommandRequested();

            Check.That(result).IsEmpty();
        }


        [Test]
        public void
            when_two_different_commandRequesteds_are_received_and_systemTicked_is_received_it_should_return_all_commandRequested()
        {
            var sut = CreateSUT();
            var cr1 = CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.1f).Value, TimeSpan.Zero);
            var cr2 = CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromMilliseconds(1));
            sut.ReceivedCommandRequested(cr1);
            sut.ReceivedCommandRequested(cr2);

            var result = sut.ReleaseCommandRequested().ToArray();

            Check.That(result).ContainsExactly(cr1, cr2);
        }

        [Test]
        public void
            when_similar_commandRequested_are_received_and_systemTicked_is_received_it_should_return_the_last_received()
        {
            var sut = CreateSUT();
            var urn = fake_urn._throttle;
            var cr1 = CommandRequested.Create(urn, Percentage.FromFloat(0.1f).Value, TimeSpan.Zero);
            var cr2 = CommandRequested.Create(urn, Percentage.FromFloat(0.4f).Value, TimeSpan.Zero);
            sut.ReceivedCommandRequested(cr1);
            sut.ReceivedCommandRequested(cr2);

            var result = sut.ReleaseCommandRequested().ToArray();

            Check.That(result).ContainsExactly(cr1);
        }


        [TestCase("TwoDifferentCommandRequest")]
        [TestCase("AggregatablePercentageCommandRequested")]
        [TestCase("AggregatableTemperatureCommandRequested")]
        [TestCase("IntertwinedCommandRequested")]
        [TestCase("OnlyNotAggregatableCommandRequested")]
        [TestCase("NotAggregatableCommandRequestedWithUnorderedAt")]
        [TestCase("AggregatableCommandRequestedWithUnorderedAt")]
        [TestCase("AggregatableCommandRequestedWithSameAt")]
        [TestCase("AggregatableCommandRequestedWithDifferentUrn")]
        public void
            when_commandRequested_and_systemTicked_are_received_it_should_return_the_lasts_received_and_keep_order(
                string id)
        {
            var sut = CreateSUT();

            foreach (var commandRequested in GetSampleCommandRequest(id).sended)
            {
                sut.ReceivedCommandRequested(commandRequested);
            }

            var result = sut.ReleaseCommandRequested().ToArray();
            Check.That(result).ContainsExactly(GetSampleCommandRequest(id).expected);
        }


        private static (CommandRequested[] sended, CommandRequested[] expected) GetSampleCommandRequest(string id)
        {
            return id switch
            {
                "TwoDifferentCommandRequest" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.2f), TimeSpan.Zero),
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(1))
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.2f), TimeSpan.Zero),
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(1))
                    }
                ),
                "AggregatablePercentageCommandRequested" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero),
                        CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.1f).Value, TimeSpan.FromTicks(1)),
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._throttle, Percentage.FromFloat(0.1f).Value, TimeSpan.FromTicks(1))
                    }
                ),
                "AggregatableTemperatureCommandRequested" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.2f), TimeSpan.Zero),
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(1)),
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(1))
                    }
                ),
                "IntertwinedCommandRequested" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.2f), TimeSpan.Zero),
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._switch, Position.B, TimeSpan.FromTicks(2)),
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(3))
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._switch, Position.B, TimeSpan.FromTicks(2)),
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(3))
                    }
                ),
                "OnlyNotAggregatableCommandRequested" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._switch, Position.B, TimeSpan.FromTicks(2)),
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._switch, Position.B, TimeSpan.FromTicks(2)),
                    }
                ),
                "NotAggregatableCommandRequestedWithUnorderedAt" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(2)),
                        CommandRequested.Create(fake_urn._switch, Position.B, TimeSpan.FromTicks(1)),
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._switch, Position.B, TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._switch, Position.A, TimeSpan.FromTicks(2)),
                    }
                ),
                "AggregatableCommandRequestedWithUnorderedAt" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(3)),
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.4f), TimeSpan.FromTicks(2))
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(3))
                    }
                ),
                "AggregatableCommandRequestedWithSameAt" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(3)),
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.4f), TimeSpan.FromTicks(3))
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(3))
                    }
                ),
                "AggregatableCommandRequestedWithDifferentUrn" =>
                (
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._setPoint2, Temperature.Create(0.4f), TimeSpan.FromTicks(2)),
                        CommandRequested.Create(fake_urn._setPoint3, Temperature.Create(0.5f), TimeSpan.FromTicks(3)),
                    },
                    new[]
                    {
                        CommandRequested.Create(fake_urn._setPoint, Temperature.Create(0.3f), TimeSpan.FromTicks(1)),
                        CommandRequested.Create(fake_urn._setPoint2, Temperature.Create(0.4f), TimeSpan.FromTicks(2)),
                        CommandRequested.Create(fake_urn._setPoint3, Temperature.Create(0.5f), TimeSpan.FromTicks(3)),
                    }
                ),
                _ => throw new ArgumentException("Unknown Id")
            };
        }

        private static CommandRequestedBuffer CreateSUT()
        {
            var aggregatorMap = new Dictionary<Type, ICommandRequestedBuffer>
            {
                {typeof(Percentage), new CommandRequestedAggregatorBuffer(YoungestCommandAggregator.Combine)},
                {typeof(Temperature), new CommandRequestedAggregatorBuffer(YoungestCommandAggregator.Combine)}
            };

            return new CommandRequestedBuffer(aggregatorMap);
        }
    }
}