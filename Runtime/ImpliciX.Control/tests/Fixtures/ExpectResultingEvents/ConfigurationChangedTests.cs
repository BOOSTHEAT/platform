using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class PropertyChangedTests : SetupSubSystemTests
    {
        private ControlSystem _sut;
        private CommandRequested Closed { get; set; }
        private CommandRequested Close { get; set; }
        private CommandRequested OpenFull { get; set; }


        [SetUp]
        public void Init()
        {
            ExecutionEnvironment = new ExecutionEnvironment();
            var automaticStore = CreateSut(AutomaticStore.State.FullyOpen, new AutomaticStore(), ExecutionEnvironment);
            _sut = new ControlSystem(ExecutionEnvironment) { SubSystems = new List<IImpliciXSystem>() { automaticStore } };
            Close = EventCommandRequested(domotic.automatic_store._close, default, TestTime);
            Closed = EventCommandRequested(domotic.automatic_store._closed, default, TestTime);
            OpenFull = EventCommandRequested(domotic.automatic_store._open, Position.Full, TestTime);
        }

        [Test]
        public void on_entering_state_onstate_function_are_executed()
        {
            var resultingEvents = _sut.PlayEvents(
                    EventPropertyChanged(domotic.automatic_store.light_settings.intensity, 0.7f, TestTime),
                    Close,
                    Closed)
                .FilterEvents<CommandRequested>(domotic.automatic_store.light._intensity);

            var expectedEvents = new[]
            {
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.7f, TestTime),
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.01f, TestTime)
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void when_receives_properties_changed_during_state()
        {
            var propertyChanged = EventPropertyChanged(domotic.automatic_store.light_settings.intensity, 0.6f, TestTime);

            var expectedEvents = new DomainEvent[]
            {
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.6f, TestTime)
            };

            var resultingEvents = _sut.PlayEvents(Close, Closed, propertyChanged);

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void when_receives_different_propertieschanged_twice_during_state()
        {
            var propertyChanged = EventPropertyChanged(domotic.automatic_store.light_settings.intensity, 0.6f, TestTime);
            var propertyChanged2 = EventPropertyChanged(domotic.automatic_store.light_settings.intensity, 0.7f, TestTime);
            var expectedEvents = new[]
            {
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.6f, TestTime),
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.7f, TestTime),
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.01f, TestTime),
            };

            var resultingEvents =
                _sut.PlayEvents(Close, Closed, propertyChanged, propertyChanged2)
                    .FilterEvents<CommandRequested>(domotic.automatic_store.light._intensity)
                    .ToArray();

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void when_stop_receives_configuration_after_change_state()
        {
            var propertiesChanged = EventPropertyChanged(domotic.automatic_store.light_settings.intensity, 0.6f, TestTime);
            var propertiesChanged2 = EventPropertyChanged(domotic.automatic_store.light_settings.intensity, 0.7f, TestTime);

            var resultingEvents =
                _sut.PlayEvents(Close, Closed, propertiesChanged, OpenFull, propertiesChanged2)
                    .FilterEvents<CommandRequested>(domotic.automatic_store.light._intensity)
                    .ToArray();

            var expectedEvents = new[]
            {
                EventCommandRequested(domotic.automatic_store.light._intensity, 0.6f, TestTime)
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }
    }
}