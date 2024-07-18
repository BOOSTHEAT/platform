using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.Tests.Examples.Bug4918;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Control.Tests.TestUtilities.ControlEventHelper;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.Bugs
{
    [TestFixture]
    public class Bug4918Tests : SetupSubSystemTests
    {
        [Test]
        public void bug_4918()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new SubsystemA(), new SubsystemB());

            var resultingEvents = sut.PlayEventsWithLogs(EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.subsystem_a.threshold, Percentage.FromFloat(0.1f).Value)
            }, TimeSpan.Zero));
            Check.That(resultingEvents.Length).IsEqualTo(0);

            resultingEvents = sut.PlayEventsWithLogs(EventCommandRequested(examples.subsystem_a._activate, default, TestTime),
                EventCommandRequested(examples.subsystem_b._activate, default, TestTime));
            resultingEvents = sut.PlayEventsWithLogs(resultingEvents);
            Check.That(resultingEvents.Length).IsEqualTo(0);

            resultingEvents = sut.PlayEventsWithLogs(EventPropertyChanged(examples.subsystem_a.needs, Percentage.FromFloat(0.2f).Value, TestTime));
            Check.That(resultingEvents).ContainsExactlyWithoutOrder(
                EventCommandRequested(examples.subsystem_b._start, default(NoArg), TestTime),
                EventPropertyChanged(examples.subsystem_a.state, SubsystemState.Create(SubsystemA.State.A2), TestTime),
                EventStateChanged(examples.subsystem_a.Urn, new Enum[] { SubsystemA.State.A2 }, TestTime)
            );

            var resultingEventsAfterNeeds = sut.PlayEventsWithLogs(EventPropertyChanged(examples.subsystem_a.needs, Percentage.FromFloat(0.001f).Value, TestTime));
            resultingEvents = sut.PlayEventsWithLogs(
                resultingEvents
                    .Concat(resultingEventsAfterNeeds)
                    .Append(EventPropertyChanged(examples.percentage, Percentage.FromFloat(0.0f).Value, TestTime)).ToArray());

            Check.That(resultingEvents).ContainsExactlyWithoutOrder(
                EventStateChanged(examples.subsystem_a.Urn, new Enum[] { SubsystemA.State.A1 }, TestTime),
                EventPropertyChanged(examples.subsystem_a.state, SubsystemState.Create(SubsystemA.State.A1), TestTime),
                EventStateChanged(examples.subsystem_b.Urn, new Enum[] { SubsystemB.State.B2 }, TestTime),
                EventPropertyChanged(examples.subsystem_b.state, SubsystemState.Create(SubsystemB.State.B2), TestTime)
            );

            resultingEvents = sut.PlayEventsWithLogs(resultingEvents);
            Check.That(resultingEvents).HasSize(0);
        }

        [Test]
        public void bug_4918_fix()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new SubsystemAFix(), new SubsystemBFix());

            var resultingEvents =
                sut.PlayEventsWithLogs(EventPropertyChanged(new (Urn urn, object value)[] { (examples.subsystem_a.threshold, Percentage.FromFloat(0.1f).Value) },
                    TimeSpan.Zero));
            Check.That(resultingEvents.Length).IsEqualTo(0);

            resultingEvents = sut.PlayEventsWithLogs(EventCommandRequested(examples.subsystem_a._activate, default, TestTime),
                EventCommandRequested(examples.subsystem_b._activate, default, TestTime));
            resultingEvents = sut.PlayEventsWithLogs(resultingEvents);
            Check.That(resultingEvents.Length).IsEqualTo(0);

            resultingEvents = sut.PlayEventsWithLogs(EventPropertyChanged(examples.subsystem_a.needs, Percentage.FromFloat(0.2f).Value, TestTime));
            Check.That(resultingEvents).ContainsExactlyWithoutOrder(
                EventPropertyChanged(TestTime,
                    (examples.subsystem_a.state, SubsystemState.Create(SubsystemAFix.State.A2)),
                    (examples.subsystem_b.sync_state, subsystem_b.SyncState.StartRequested)),
                EventStateChanged(examples.subsystem_a.Urn, new Enum[] { SubsystemAFix.State.A2 }, TestTime)
            );

            var resultingEventsAfterNeeds = sut.PlayEventsWithLogs(EventPropertyChanged(examples.subsystem_a.needs, Percentage.FromFloat(0.001f).Value, TestTime));
            resultingEvents = sut.PlayEventsWithLogs(
                resultingEvents
                    .Concat(resultingEventsAfterNeeds)
                    .Append(EventPropertyChanged(examples.percentage, Percentage.FromFloat(0.0f).Value, TestTime)).ToArray());
            Check.That(resultingEvents).Contains(
                EventStateChanged(examples.subsystem_b.Urn, new Enum[] { SubsystemBFix.State.B2 }, TestTime),
                EventPropertyChanged(TestTime,
                    (examples.subsystem_b.sync_state, subsystem_b.SyncState.Started),
                    (examples.subsystem_b.state, SubsystemState.Create(SubsystemBFix.State.B2))),
                EventPropertyChanged(TestTime, (examples.dummy, Percentage.FromFloat(0.0f).Value))
            );

            resultingEvents = sut.PlayEventsWithLogs(resultingEvents);

            Check.That(resultingEvents).HasSize(0);
        }
    }

    public static class ImpliciXSystemExtensions
    {
        public static DomainEvent[] PlayEventsWithLogs(this IImpliciXSystem @this, params DomainEvent[] @events)
        {
            var resultingEvents = new List<DomainEvent>();

            foreach (var @event in @events)
            {
                Console.WriteLine($"Incoming event : {@event}");
                resultingEvents.AddRange(@this.PlayEvent(@event));
            }

            foreach (var @event in resultingEvents)
            {
                Console.WriteLine($"Outgoing event : {@event}");
            }

            Console.WriteLine();
            return resultingEvents.ToArray();
        }

        private static IEnumerable<DomainEvent> PlayEvent(this IImpliciXSystem @this, DomainEvent @event) =>
            @this.HandleDomainEvent(@event);

        public static void ContainsExactlyWithoutOrder<T>(this ICheck<IEnumerable<T>> check, params T[] expectedValues)
        {
            check.Contains(expectedValues);
            check.HasSize(expectedValues.Length);
        }
    }
}