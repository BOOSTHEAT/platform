using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class ComputationTests : SetupSubSystemTests
    {
        public ControlSystem Sut;

        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(domotic).Assembly);
        }

        private void Init(FanController.State state)
        {
            ExecutionEnvironment = new ExecutionEnvironment();
            var fanController = CreateSut(state, new FanController(), ExecutionEnvironment);
            Sut = new ControlSystem(ExecutionEnvironment) { SubSystems = new List<IImpliciXSystem>() { fanController } };
        }
        [Test]
        public void should_compute_polynomial1()
        {
            Init(FanController.State.Stable);
            var functionDefinition = new FunctionDefinition(new[] { ("a0", 0.0f), ("a1", 0.01f) });

            var resultingEvents = Sut.PlayEvents(
                EventCommandRequested(domotic.fancontroller._activate, default(NoArg), TestTime),
                EventPropertyChanged(domotic.fancontroller.compute_throttle_polynomial, functionDefinition, TestTime),
                EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 45f, TestTime));

            var expectedEvents = new CommandRequested[]
            {
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 0.45f, TestTime)
            };
            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_compute_pid_2nd_variable_triggers()
        {
            Init(FanController.State.Off);
            var functionDefinition = new FunctionDefinition(new[]
            {
                ("Kp", 1f), ("Ki", 0f), ("Kd", 0f), ("Zm", -1f), ("MinValue", 0f), ("MaxValue", 50f), ("Slope", 1f), ("Biais", 0f)
            });

            var resultingEvents = Sut.PlayEvents(
                EventPropertyChanged(domotic.fancontroller.compute_throttle_pid, functionDefinition, TestTime),
                EventPropertyChanged(domotic.fancontroller.setpoint_temperature, 5f, TestTime),
                EventPropertyChanged(domotic.fancontroller.fan3._throttle.measure, 1f, TestTime),
                EventCommandRequested(domotic.fancontroller._start, default(NoArg), TestTime),
                EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 6f, TestTime)
            );

            DomainEvent[] expectedEvents =
            {
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 1f, TestTime)
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_compute_pid_1st_variable_triggers()
        {
            Init(FanController.State.Off);
            var functionDefinition = new FunctionDefinition(new[]
            {
                ("Kp", 1f), ("Ki", 0f), ("Kd", 0f), ("Zm", -1f), ("MinValue", 0f), ("MaxValue", 50f), ("Slope", 1f), ("Biais", 0f)
            });
            var resultingEvents = Sut.PlayEvents(
                EventPropertyChanged(domotic.fancontroller.compute_throttle_pid, functionDefinition, TestTime),
                EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 6f, TestTime.Add(TimeSpan.FromSeconds(1))),
                EventPropertyChanged(domotic.fancontroller.fan3._throttle.measure, 1f, TestTime.Add(TimeSpan.FromSeconds(1))),
                EventCommandRequested(domotic.fancontroller._start, default(NoArg), TestTime.Add(TimeSpan.FromSeconds(1))),
                EventPropertyChanged(domotic.fancontroller.setpoint_temperature, 5f, TestTime.Add(TimeSpan.FromSeconds(2)))
            );

            DomainEvent[] expectedEvents =
            {
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 1f, TestTime)
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_keep_previous_pid_instance()
        {
            Init(FanController.State.Off);

            var functionDefinition = new FunctionDefinition(new[]
            {
                ("Kp", 2f), ("Ki", 4f), ("Kd", 0f), ("Zm", -1f), ("MinValue", 0f), ("MaxValue", 100f), ("Slope", 1f), ("Biais", 0f)
            });

            var resultingEvents = Sut.PlayEvents(
                    EventPropertyChanged(domotic.fancontroller.compute_throttle_pid, functionDefinition, Time(0)),
                    EventPropertyChanged(domotic.fancontroller.setpoint_temperature, 5f, Time(0)),
                    EventPropertyChanged(domotic.fancontroller.fan3._throttle.measure, 1f, Time(0)),
                    EventCommandRequested(domotic.fancontroller._start, default(NoArg), Time(0)),
                    EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 12f, Time(1)),
                    EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 12f, Time(2)),
                    EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 12f, Time(3)),
                    EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 12f, Time(4))
                ).FilterEvents<CommandRequested>(domotic.fancontroller.fan3._throttle)
                .ToArray();


            var expectedEvents = new[]
            {
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 1f, Time(0)),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 43f, Time(1)),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 71f, Time(2)),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 99f, Time(3)),
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_reset_pid_instance()
        {
            Init(FanController.State.Off);
            var functionDefinition = new FunctionDefinition(new[]
            {
                ("Kp", 2f), ("Ki", 4f), ("Kd", 0f), ("Zm", -1f), ("MinValue", 0f), ("MaxValue", 100f), ("Slope", 1f), ("Biais", 0f)
            });

            var resultingEvents = Enumerable.Range(0, 3).SelectMany(n =>
                    Sut.PlayEvents(
                        EventPropertyChanged(domotic.fancontroller.compute_throttle_pid, functionDefinition, Time(n)),
                        EventPropertyChanged(domotic.fancontroller.setpoint_temperature, 5f, Time(n)),
                        EventPropertyChanged(domotic.fancontroller.fan3._throttle.measure, 1f, Time(n)),
                        EventCommandRequested(domotic.fancontroller._start, default(NoArg), Time(n)),
                        EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 12f, Time(n)),
                        EventCommandRequested(domotic.fancontroller._stop, default(NoArg), Time(n))
                    ))
                .FilterEvents<CommandRequested>(domotic.fancontroller.fan3._throttle)
                .ToArray();

            var expectedEvents = new[]
            {
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 1f, TestTime),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 1f, TestTime),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 43f, TestTime),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 1f, TestTime),
                EventCommandRequested(domotic.fancontroller.fan3._throttle, 43f, TestTime),
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        private TimeSpan Time(int n)
        {
            return TestTime.Add(TimeSpan.FromSeconds(n));
        }

        [Test]
        public void should_send_property_change()
        {
            Init(FanController.State.Stable);

            var resultingEvents = Sut.PlayEvents(
                EventCommandRequested(domotic.fancontroller._activate, default(NoArg), TestTime),
                EventPropertyChanged(domotic.fancontroller.setpoint_temperature, 5f, TestTime),
                EventCommandRequested(domotic.fancontroller._beforeStop, default(NoArg), TestTime),
                EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 12f, TestTime)
            );
            var expectedEvents = new[]
            {
                EventPropertyChanged(domotic.fancontroller.fan3.threshold, 7f, TestTime)
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_not_be_computed_when_trigger_urns_do_not_change()
        {
            Init(FanController.State.Off);
            var functionDefinition = new FunctionDefinition(new[]
            {
                ("Kp", 1f), ("Ki", 0f), ("Kd", 0f), ("Zm", -1f), ("MinValue", 0f), ("MaxValue", 50f), ("Slope", 1f)
            });
            Sut.PlayEvents(
                EventPropertyChanged(domotic.fancontroller.compute_throttle_pid, functionDefinition, TestTime),
                EventPropertyChanged(domotic.fancontroller.thermometer.temperature, 6f, TestTime),
                EventPropertyChanged(domotic.fancontroller.fan3._throttle.measure, 0f, TestTime),
                EventCommandRequested(domotic.fancontroller._start, default(NoArg), TestTime),
                EventPropertyChanged(domotic.fancontroller.setpoint_temperature, 5f, TestTime)
            );

            var resultingEvents = Sut.PlayEvents(
                EventPropertyChanged(domotic.fancontroller.fan3._throttle.measure, 1f, TestTime)
            );


            Check.That(resultingEvents).IsEmpty();
        }


        [Test]
        public void should_compute_substract()
        {
            Init(FanController.State.Off);

            var changed1 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero,
                (domotic.fancontroller.thermometer.temperature, Temperature.Create(2f)));
            var changed2 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero, (domotic.fancontroller.setpoint_temperature, Temperature.Create(4f)));

            var resultingEvents = Sut.PlayEvents(EventCommandRequested(domotic.fancontroller._start, default, TestTime), changed1, changed2);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(domotic.fancontroller.delta, Temperature.Create(2f), TimeSpan.Zero),
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void should_compute_an_onstate_defined_function_on_entry()
        {
            Init(FanController.State.Off);

            var changed1 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero,
                (domotic.fancontroller.thermometer.temperature, Temperature.Create(2f)));
            var changed2 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero, (domotic.fancontroller.setpoint_temperature, Temperature.Create(4f)));
            var start = EventCommandRequested(domotic.fancontroller._start, default, TestTime);
            var stop = EventCommandRequested(domotic.fancontroller._stop, default, TestTime);

            Sut.PlayEvents(changed1, changed2);

            var resultingEvents = Sut.PlayEvents(start, stop, start).FilterEvents<PropertiesChanged>(domotic.fancontroller.delta);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(new (Urn, object)[]
                {
                    (domotic.fancontroller.starting_temperature, Temperature.Create(2f)),
                    (domotic.fancontroller.state, SubsystemState.Create(FanController.State.VeryUnstable)),
                    (domotic.fancontroller.delta, Temperature.Create(2f))
                }, TimeSpan.Zero),
                EventPropertyChanged(new (Urn, object)[]
                {
                    (domotic.fancontroller.starting_temperature, Temperature.Create(2f)),
                    (domotic.fancontroller.state, SubsystemState.Create(FanController.State.VeryUnstable)),
                    (domotic.fancontroller.delta, Temperature.Create(2f))
                }, TimeSpan.Zero)
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_compute_an_onentry_defined_function_only_when_enters_state()
        {
            Init(FanController.State.Off);
            var changed1 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero,
                (domotic.fancontroller.thermometer.temperature, Temperature.Create(2f)));
            var start = EventCommandRequested(domotic.fancontroller._start, default, TestTime);
            var resultingEvents = Sut.PlayEvents(changed1, start).FilterEvents<PropertiesChanged>(domotic.fancontroller.starting_temperature).ToArray();
            var eventPropertyChanged = EventPropertyChanged(new (Urn, object)[]
            {
                (domotic.fancontroller.starting_temperature, Temperature.Create(2f)),
                (domotic.fancontroller.state, SubsystemState.Create(FanController.State.VeryUnstable)),
            }, TimeSpan.Zero);
            Check.That(resultingEvents)
                .Contains(eventPropertyChanged);

            var changed2 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero,
                (domotic.fancontroller.thermometer.temperature, Temperature.Create(3f)));
            var resultingEvents1 = Sut.PlayEvents(changed2).FilterEvents<PropertiesChanged>(domotic.fancontroller.starting_temperature).ToArray();
            Check.That(resultingEvents1).CountIs(0);
        }


        [Test]
        public void should_not_compute_function_when_at_least_one_variable_is_not_available()
        {
            Init(FanController.State.Off);

            var changed1 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero,
                (domotic.fancontroller.thermometer.temperature, Temperature.Create(2f)));

            var start = EventCommandRequested(domotic.fancontroller._start, default, TestTime);

            Sut.PlayEvents(start);
            var resultingEvents = Sut.PlayEvents(changed1);

            Check.That(resultingEvents.Any(de => de is PropertiesChanged pc && pc.ContainsProperty(domotic.fancontroller.delta))).IsFalse();
        }
    }
}