using System;
using System.Linq;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.Control.Tests.TestUtilities.ControlEventHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class PublishCurrentStateTest : SetupSubSystemTests
    {
        public CommandRequested Closed { get; set; }
        public CommandRequested Close { get; set; }

        
        [SetUp]
        public void Init()
        {
            Close = EventCommandRequested(domotic.automatic_store._close, default,TestTime);
            Closed = EventCommandRequested(domotic.automatic_store._closed, default,TestTime);
        }

        [Test]
        public void should_publish_the_initial_state_when_activated()
        {
            var sut = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore());
            var resultingEvents =
                sut.PlayEvents(EventCommandRequested(domotic.automatic_store._activate, default(NoArg), TestTime));
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyOpen), TestTime),
            };
            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_publish_the_initial_state_of_composite_state_entry()
        {
            var computer = new Computer();
            var sut = CreateSut(Computer.State.Shutdown, computer);
            var resultingEvents = sut.PlayEvents(EventCommandRequested(devices.computer._start, default(NoArg), TestTime));
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(computer.StateUrn, SubsystemState.Create(Computer.State.KernelLoading), TestTime)
            };
            resultingEvents = resultingEvents.FilterEvents<PropertiesChanged>(computer.StateUrn).ToArray();
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_publish_the_current_state_when_it_changes()
        {
            var automaticStore = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore());
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.ClosureInProgress), TestTime),
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyClosed), TestTime),
            };

            var resultingEvents = automaticStore.PlayEvents(Close, Closed);
            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_publish_current_state_and_composite_state()
        {
            var sut = CreateSut(NestedComposite.State.A, new NestedComposite());
            var expected = new DomainEvent[]
            {
                EventStateChanged(examples.nested_composites.Urn, new Enum[] {NestedComposite.State.A}, TestTime),
                EventStateChanged(examples.nested_composites.Urn, new Enum[] {NestedComposite.State.B, NestedComposite.State.Ba, NestedComposite.State.Baa}, TestTime)
            };
            var activateEvent = EventCommandRequested(examples.nested_composites._activate, default, TestTime);
            var goToBEvent = EventCommandRequested(examples.nested_composites._tb, default, TestTime);
            var resultingEvents = sut.PlayEvents(activateEvent, goToBEvent);
            Check.That(resultingEvents).Contains(expected);
        }

        [Test]
        public void should_publish_fragment_state()
        {
            var sut = CreateSut(IncludeFragment.State.A, new IncludeFragment());
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.include_fragment.state, SubsystemState.Create(IncludeFragment.State.Aif), TestTime),
            };
            var activateEvent = EventCommandRequested(examples.include_fragment._activate, default, TestTime);
            var resultingEvents = sut.PlayEvents(activateEvent);
            Check.That(resultingEvents).Contains(expectedEvents);
        }
    }
}