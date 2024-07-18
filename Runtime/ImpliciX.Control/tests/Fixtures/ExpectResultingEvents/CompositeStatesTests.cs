using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class CompositeStatesTests : SetupSubSystemTests
    {
        public ControlSystem sut;

        private (SubSystem<Computer.State>, Computer) Init(Computer.State state, bool ordered)
        {
            ExecutionEnvironment = new ExecutionEnvironment();
            var computerDef = GetComputer(ordered);
            var computer = CreateSut(state, computerDef, ExecutionEnvironment);
            sut = new ControlSystem(ExecutionEnvironment) { SubSystems = new List<IImpliciXSystem>() { computer } };
            return (computer, computerDef);
        }

        private Computer GetComputer(bool ordered)
        {
            return ordered
                ? new Computer()
                : new Computer(TestTime, false);
        }


        [TestCase(true)]
        [TestCase(false)]
        public void on_entry_composite_state_is_executed(bool orderedSut)
        {
            Init(Computer.State.Shutdown, orderedSut);
            var resultingEvents = sut.PlayEvents(EventCommandRequested(devices.computer._start, default, TestTime));

            var expectedEvents = new[]
            {
                EventCommandRequested(devices.computer.led._switch, Switch.On, TestTime),
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void on_entry_composite_state_enters_in_its_initial_substate(bool orderedSut)
        {
            var (computer, computerDef) = Init(Computer.State.Shutdown, orderedSut);
            sut.PlayEvents(EventCommandRequested(devices.computer._start, default, TestTime));
            Check.That(computer.CurrentState).IsEqualTo(Computer.State.KernelLoading);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void on_state_actions_defined_on_composite_state_are_executed(bool orderedSut)
        {
            Init(Computer.State.Shutdown, orderedSut);
            var resultingEvents = sut.PlayEvents(
                EventPropertyChanged(devices.computer.fan_speed, 0.2f, TestTime),
                EventCommandRequested(devices.computer._start, default, TestTime),
                EventPropertyChanged(devices.computer.fan_speed, 0.6f, TestTime));

            var expectedEvents = new CommandRequested[]
            {
                EventCommandRequested(devices.computer.fan2._throttle, 0.2f, TestTime),
                EventCommandRequested(devices.computer.fan2._throttle, 0.6f, TestTime),
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void on_entry_of_substate_is_executed(bool orderedSut)
        {
            Init(Computer.State.KernelLoading, orderedSut);
            var resultingEvents =
                sut.PlayEvents(EventCommandRequested(devices.computer._mount, default(NoArg), TestTime));

            var expectedEvents = new[]
            {
                EventCommandRequested(devices.computer._buzz, Switch.On, TestTime)
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void on_state_action_substate_is_executed(bool orderedSut)
        {
            Init(Computer.State.Shutdown, orderedSut);
            var resultingEvents = sut.PlayEvents(
                EventPropertyChanged(devices.computer.fan_speed, 0.2f, TestTime),
                EventCommandRequested(devices.computer._start, default, TestTime),
                EventPropertyChanged(devices.computer.fan_speed, 0.6f, TestTime));

            var expectedEvents = new CommandRequested[]
            {
                EventCommandRequested(devices.computer.fan2._throttle, 0.2f, TestTime),
                EventCommandRequested(devices.computer.fan2._throttle, 0.6f, TestTime),
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void on_state_functions_substate_is_executed(bool orderedSut)
        {
            Init(Computer.State.Shutdown, orderedSut);
            var functionDefinition = new FunctionDefinition(new[] { ("a0", 0f), ("a1", 1f) });

            var resultingEvents = sut.PlayEvents(
                EventPropertyChanged(devices.computer.compute_constant, functionDefinition, TestTime),
                EventCommandRequested(devices.computer._start, default(NoArg), TestTime),
                EventPropertyChanged(devices.computer.variable, 0.5f, TestTime),
                EventCommandRequested(devices.computer._mount, default(NoArg), TestTime),
                EventPropertyChanged(devices.computer.variable, 0.8f, TestTime)
            );

            var expectedEvents = new CommandRequested[]
            {
                EventCommandRequested(devices.computer._send, 0.50f, TestTime),
                EventCommandRequested(devices.computer._send, 0.80f, TestTime),
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void full_scenario_without_function(bool orderedSut)
        {
            Init(Computer.State.Shutdown, orderedSut);
            var resultingEvents = sut.PlayEvents(
                    EventPropertyChanged(devices.computer.fan_speed, 0.2f, TestTime), //KERNEL LOADING
                    EventCommandRequested(devices.computer._start, default(NoArg), TestTime),
                    EventPropertyChanged(devices.computer.fan_speed, 0.5f, TestTime),
                    EventCommandRequested(devices.computer._mount, default(NoArg), TestTime), //MOUNTING FS
                    EventPropertyChanged(devices.computer.fan_speed, 0.8f, TestTime),
                    EventCommandRequested(devices.computer._mounted, default(NoArg), TestTime), //BOOTED
                    EventCommandRequested(devices.computer._powerOff, default(NoArg), TestTime)
                )
                .FilterEvents<CommandRequested>()
                .ToArray();

            var expectedEvents = new CommandRequested[]
            {
                // KERNEL LOADING
                EventCommandRequested(devices.computer.led._switch, Switch.On, TestTime),
                EventCommandRequested(devices.computer.fan._throttle, 0.20f, TestTime),
                EventCommandRequested(devices.computer.fan2._throttle, 0.20f, TestTime),
                EventCommandRequested(devices.computer.fan._throttle, 0.50f, TestTime),
                EventCommandRequested(devices.computer.fan2._throttle, 0.50f, TestTime),

                // MOUNTING FS
                EventCommandRequested(devices.computer._buzz, Switch.On, TestTime),
                EventCommandRequested(devices.computer.fan._throttle, 0.80f, TestTime),

                // BOOTED
                EventCommandRequested(devices.computer._buzz, Switch.Off, TestTime),
                EventCommandRequested(devices.computer._buzz, Switch.On, TestTime)
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void multi_composite()
        {
            InitNestedComposite(NestedComposite.State.A);
            var resultingEvents = sut.PlayEvents(
                    EventCommandRequested(examples.nested_composites._tb, default(NoArg), TestTime),
                    EventPropertyChanged(examples.nested_composites.value, "WhenInBaa", TestTime),
                    EventCommandRequested(examples.nested_composites._tbab, default(NoArg), TestTime),
                    EventPropertyChanged(examples.nested_composites.value, "WhenInBab", TestTime),
                    EventCommandRequested(examples.nested_composites._tbb, default(NoArg), TestTime),
                    EventPropertyChanged(examples.nested_composites.value, "WhenInBb", TestTime),
                    EventCommandRequested(examples.nested_composites._tb, default(NoArg), TestTime)
                )
                .FilterEvents<CommandRequested>()
                .ToArray();

            var expectedEvents = new CommandRequested[]
            {
                EventCommandRequested(examples.nested_composites._cmd1, "WhenInBaa", TestTime),
                EventCommandRequested(examples.nested_composites._cmd2, "WhenInBaa", TestTime),
                EventCommandRequested(examples.nested_composites._cmd1, "WhenInBab", TestTime),
                EventCommandRequested(examples.nested_composites._cmd2, "WhenInBab", TestTime),
                EventCommandRequested(examples.nested_composites._cmd1, "WhenInBb", TestTime),
                EventCommandRequested(examples.nested_composites._cmd1, "WhenInBb", TestTime),
                EventCommandRequested(examples.nested_composites._cmd2, "WhenInBb", TestTime)
            };
            Check.That(((SubSystem<NestedComposite.State>) sut.SubSystems[0]).CurrentState).IsEqualTo(NestedComposite.State.Baa);
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        private void InitNestedComposite(NestedComposite.State state)
        {
            ExecutionEnvironment = new ExecutionEnvironment();
            var mc = CreateSut(state, new NestedComposite(), ExecutionEnvironment);
            sut = new ControlSystem(ExecutionEnvironment) { SubSystems = new List<IImpliciXSystem>() { mc } };
        }
    }
}