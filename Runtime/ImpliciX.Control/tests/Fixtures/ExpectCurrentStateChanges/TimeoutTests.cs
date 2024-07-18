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

namespace ImpliciX.Control.Tests.Fixtures.ExpectCurrentStateChanges
{
    [TestFixture]
    public class TimeoutTests : SetupSubSystemTests
    {
        [Test]
        public void when_receives_timeout()
        {
            var close = EventCommandRequested(domotic.automatic_store._close, default, TestTime);
            var automaticStore = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore());

            var resultingEvents = automaticStore.PlayEvents(close);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(domotic.automatic_store.state,
                    SubsystemState.Create(AutomaticStore.State.ClosureInProgress), TestTime),
                EventCommandRequested(domotic.automatic_store._toDriver, ".", TestTime),
                NotifyOnTimeoutRequested.Create(domotic.automatic_store.Timer, TestTime)
            };

            Check.That(resultingEvents).Contains(expectedEvents);

            automaticStore.PlayEvents(EventTimeoutOccured(domotic.automatic_store.Timer, TestTime,
                resultingEvents.FilterEvents<NotifyOnTimeoutRequested>().First().EventId));

            Check.That(automaticStore.CurrentState).IsEqualTo(AutomaticStore.State.FullyClosed);
        }

        [Test]
        public void when_A_requests_timeout_and_B_consumes_it()
        {
            var toggle = EventCommandRequested(examples.timeout_subsystem.toggle, default, TestTime);

            var sut = CreateSut(State.A, new TimeoutSubsystemAThenB());

            var actualEvents = sut.Activate();

            var timeoutOccured = EventTimeoutOccured(examples.timeout_subsystem.timeoutUrn, TestTime,
                actualEvents.FilterEvents<NotifyOnTimeoutRequested>().First().EventId);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.A), TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] { State.A }, TestTime),
                NotifyOnTimeoutRequested.Create(examples.timeout_subsystem.timeoutUrn, TestTime)
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);

            actualEvents = sut.PlayEvents(toggle);

            expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.B), TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] { State.B }, TestTime),
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);

            actualEvents = sut.PlayEvents(timeoutOccured);

            expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.C), TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] { State.C }, TestTime),
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);
        }

        [Test]
        public void when_A_requests_timeout_and_sub_state_B_consumes_it()
        {
            var sut = CreateSut(State.A, new TimeoutSubsystemComposite());
            var actualEvents = sut.Activate();
            var timeoutOccured = EventTimeoutOccured(examples.timeout_subsystem.timeoutUrn, TestTime,
                actualEvents.FilterEvents<NotifyOnTimeoutRequested>().First().EventId);

            var expectedEvents = new DomainEvent[]
            {
                NotifyOnTimeoutRequested.Create(examples.timeout_subsystem.timeoutUrn, TestTime),
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.B), TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] { State.A, State.B }, TestTime)
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);

            actualEvents = sut.PlayEvents(timeoutOccured);

            expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.C), TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] { State.A, State.C }, TestTime),
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);
        }

        [Test]
        public void when_subsystem_A_requests_timeout_and_subsystem_B_consumes_it()
        {
            CreateSut(State.A, new TimeoutSubsystemA());
            var sut = new ControlSystem()
            {
                SubSystems = new IImpliciXSystem[]
                    { CreateSut(State.A, new TimeoutSubsystemA()), CreateSut(State.B, new TimeoutSubsystemB()) }
            };

            var actualEvents = sut.Activate();

            var timeoutOccured = EventTimeoutOccured(examples.timeout_subsystem.timeoutUrn, TestTime,
                actualEvents.FilterEvents<NotifyOnTimeoutRequested>().First().EventId);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem_a.state, SubsystemState.Create(State.A), TestTime),
                EventStateChanged(examples.timeout_subsystem_a.Urn, new Enum[] { State.A }, TestTime),
                NotifyOnTimeoutRequested.Create(examples.timeout_subsystem_a.timeoutUrn, TestTime),
                EventPropertyChanged(examples.timeout_subsystem_b.state, SubsystemState.Create(State.B), TestTime),
                EventStateChanged(examples.timeout_subsystem_b.Urn, new Enum[] { State.B }, TestTime),
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);

            actualEvents = sut.PlayEvents(timeoutOccured);

            expectedEvents = new DomainEvent[]
            {
                EventStateChanged(examples.timeout_subsystem_b.Urn, new Enum[] { State.C }, TestTime),
                EventPropertyChanged(examples.timeout_subsystem_b.state, SubsystemState.Create(State.C), TestTime),
            };

            Check.That(actualEvents).IsEqualTo(expectedEvents);
        }
    }
}