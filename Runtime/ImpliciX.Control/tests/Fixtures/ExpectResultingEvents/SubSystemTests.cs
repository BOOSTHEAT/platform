using System;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.ValueObjects;
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
    public class SubSystemTests : SetupSubSystemTests
    {
        private CommandRequested Closed { get; set; }
        private CommandRequested Close { get; set; }
        private CommandRequested Activate { get; set; }

        [SetUp]
        public void Init()
        {
            Activate = EventCommandRequested(domotic.automatic_store._activate, default, TestTime);
            Close = EventCommandRequested(domotic.automatic_store._close, default, TestTime);
            Closed = EventCommandRequested(domotic.automatic_store._closed, default, TestTime);
        }

        [Test]
        public void execution_from_initial_to_final_state()
        {
            var properties = new ExecutionEnvironment();
            var automaticStore = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore(), properties);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyOpen), TestTime),
                EventStateChanged(domotic.automatic_store.Urn, new Enum[] { AutomaticStore.State.FullyOpen }, TestTime),
                EventCommandRequested(domotic.secondary_store._open, default(NoArg), TestTime),
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.ClosureInProgress), TestTime),
                EventStateChanged(domotic.automatic_store.Urn, new Enum[] { AutomaticStore.State.ClosureInProgress }, TestTime),
                NotifyOnTimeoutRequested.Create(domotic.automatic_store.Timer, TestTime),
                EventCommandRequested(domotic.automatic_store._toDriver, ".", TestTime),
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyClosed), TestTime),
                EventStateChanged(domotic.automatic_store.Urn, new Enum[] { AutomaticStore.State.FullyClosed }, TestTime),
                EventCommandRequested(domotic.secondary_store._closeWithParam, HowMuch.Full, TestTime),
                EventCommandRequested(domotic.automatic_store.light._switch, Switch.On, TestTime),
                EventPropertyChanged(domotic.automatic_store.light_settings.default_intensity, Percentage.FromFloat(0.01f).Value, TestTime),
                EventPropertyChanged(domotic.automatic_store.light_settings.intensity, Percentage.FromFloat(0.01f).Value, TestTime),
                EventCommandRequested(domotic.automatic_store.light._intensity, Percentage.FromFloat(0.01f).Value, TestTime)
            };

            var resultingEvents = automaticStore.PlayEvents(Activate, Close, Closed);

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void initial_state_actions_should_be_executed()
        {
            var automaticStore = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore());
            object[] expectedEvents =
            {
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyOpen), TestTime),
                EventStateChanged(domotic.automatic_store.Urn, new Enum[] { AutomaticStore.State.FullyOpen }, TestTime),
                EventCommandRequested(domotic.secondary_store._open, default(NoArg), TestTime),
            };

            var resultingEvents = automaticStore.PlayEvents(Activate);

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void activation_outcome_test()
        {
            var automaticStore = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore());
            var domainEvents = automaticStore.Activate();
            var expected = new DomainEvent[]
            {
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyOpen), TestTime),
                EventStateChanged(domotic.automatic_store.Urn, new Enum[] { AutomaticStore.State.FullyOpen }, TestTime),
                EventCommandRequested(domotic.secondary_store._open, default(NoArg), TestTime),
            };
            Check.That(domainEvents).ContainsExactly(expected);
        }
    }
}