#nullable enable
using System;
using System.Reflection;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.BrahmaBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<
    ImpliciX.RTUModbus.Controllers.BrahmaBoard.Controller,
    ImpliciX.RTUModbus.Controllers.BrahmaBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;
using State = ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;

namespace ImpliciX.RTUModbus.Controllers.Tests.BrahmaBoard
{
    [TestFixture]
    public class CanHandleTests
    {
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;
        private static readonly HardwareAndSoftwareDeviceNode OtherDeviceNode = test_model.software.fake_other_board;
        private static readonly BurnerNode GenericBurner = test_model.burner;
        private static readonly FanNode _burnerFan = test_model.burner.fan;

        [TestCase(DisabledAvailable, "test_model:commands:do_something_noarg", null, false)]
        [TestCase(DisabledAvailable, "test_model:burner:SUPPLY", PowerSupply.Off, false)]
        [TestCase(DisabledNotAvailable, "test_model:commands:do_something_noarg", null, false)]
        [TestCase(DisabledNotAvailable, "test_model:burner:SUPPLY", PowerSupply.Off, false)]
        [TestCase(EnabledNotAvailable, "test_model:commands:do_something_noarg", null, false)]
        [TestCase(EnabledNotAvailable, "test_model:burner:SUPPLY", PowerSupply.Off, false)]
        [TestCase(EnabledAvailable, "test_model:commands:do_something_noarg", null, true)]
        [TestCase(EnabledAvailable, "test_model:burner:SUPPLY", PowerSupply.Off, true)]
        [TestCase(EnabledAvailable, "test_model:burner:SUPPLY", PowerSupply.On, true)]
        [TestCase(EnabledAvailable, "test_model:burner:START_IGNITION", null, true)]
        [TestCase(EnabledAvailable, "test_model:burner:STOP_IGNITION", null, true)]
        [TestCase(EnabledAvailable, "test_model:burner:MANUAL_RESET", null, true)]
        [TestCase(EnabledAvailable, "test_model:burner:fan:THROTTLE", 1f, true)]
        public void handle_commands_supported_by_the_brahma_slave(State currentState, string commandUrn, object arg,
            bool expected)
        {
            var controller = DefineControllerInState(currentState)
                .ForSimulatedSlave(DeviceNode, GenericBurner)
                .ExecuteCommandSimulation(test_model.commands.do_something_noarg).ReturningSuccessResult()
                .ExecuteCommandSimulation(_burnerFan._throttle).ReturningSuccessResult()
                .BuildSlaveController();

            var trigger = EventCommandRequested(commandUrn, arg ?? default(NoArg), TimeSpan.Zero);
            Check.That(controller.CanHandle(trigger)).IsEqualTo(expected);
        }

        [TestCase(DisabledAvailable)]
        [TestCase(EnabledAvailable)]
        [TestCase(DisabledNotAvailable)]
        [TestCase(EnabledNotAvailable)]
        public void should_handle_PropertiesChanged_containing_slave_settings_urns(State currentState)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .WithSettingsUrns(new Urn[]
                    {
                        test_model.software.fake_daughter_board.presence,
                        test_model.measures.pressure1.measure
                    })
                    .BuildSlaveController();
            var presenceEnabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence,
                Presence.Enabled, TimeSpan.Zero);
            var presenceDisabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence,
                Presence.Disabled, TimeSpan.Zero);
            var pressureMeasure = EventPropertyChanged(test_model.measures.pressure1.measure, 2f, TimeSpan.Zero);
            Check.That(slaveController.CanHandle(presenceEnabled)).IsTrue();
            Check.That(slaveController.CanHandle(presenceDisabled)).IsTrue();
            Check.That(slaveController.CanHandle(pressureMeasure)).IsTrue();
        }

        [Test]
        public void should_not_handle_PropertiesChanged_not_containing_slave_settings_urns()
        {
            var slaveController =
                DefineControllerInState(EnabledAvailable)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .WithSettingsUrns(new Urn[] { })
                    .BuildSlaveController();
            var pressureMeasure = EventPropertyChanged(test_model.measures.pressure1.measure, 2f, TimeSpan.Zero);
            var presenceEnabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence,
                Presence.Enabled, TimeSpan.Zero);
            var presenceDisabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence,
                Presence.Disabled, TimeSpan.Zero);
            Check.That(slaveController.CanHandle(pressureMeasure)).IsFalse();
            Check.That(slaveController.CanHandle(presenceDisabled)).IsFalse();
            Check.That(slaveController.CanHandle(presenceEnabled)).IsFalse();
        }

        [TestCase(EnabledAvailable, true)]
        [TestCase(DisabledAvailable, false)]
        [TestCase(DisabledNotAvailable, false)]
        [TestCase(EnabledNotAvailable, false)]
        public void should_handle_system_ticked(State currentState, bool expected)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .WithSettingsUrns(new Urn[] { })
                    .BuildSlaveController();
            Check.That(slaveController.CanHandle(SystemTicked.Create(1000, 1)))
                .IsEqualTo(expected);
        }

        [TestCase(EnabledAvailable, typeof(FaultedDetected), true)]
        [TestCase(EnabledAvailable, typeof(NotFaultedDetected), true)]
        [TestCase(DisabledAvailable, typeof(FaultedDetected), false)]
        [TestCase(DisabledAvailable, typeof(NotFaultedDetected), false)]
        [TestCase(DisabledNotAvailable, typeof(FaultedDetected), false)]
        [TestCase(DisabledNotAvailable, typeof(NotFaultedDetected), false)]
        [TestCase(EnabledNotAvailable, typeof(FaultedDetected), false)]
        [TestCase(EnabledNotAvailable, typeof(NotFaultedDetected), false)]
        public void should_handle_private_events(State currentState, Type eventType, bool expected)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .WithSettingsUrns(new Urn[] { })
                    .BuildSlaveController();

            var trigger = (DomainEvent)Activator.CreateInstance(
                eventType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object?[] { test_model.software.fake_daughter_board, test_model.burner }, null)!;

            Check.That(slaveController.CanHandle(trigger)).IsEqualTo(expected);
        }

        [TestCase(EnabledAvailable, "test_model:burner:ignition_settings:ignition_period", true)]
        [TestCase(EnabledAvailable, "test_model:burner:ignition_settings:ignition_supplying_delay", true)]
        [TestCase(EnabledAvailable, "test_model:burner:ignition_settings:ignition_period", true)]
        [TestCase(EnabledNotAvailable, "test_model:burner:ignition_settings:ignition_period", false)]
        [TestCase(EnabledNotAvailable, "test_model:burner:ignition_settings:ignition_supplying_delay", false)]
        [TestCase(EnabledNotAvailable, "test_model:burner:ignition_settings:ignition_reset_delay", false)]
        [TestCase(DisabledNotAvailable, "test_model:burner:ignition_settings:ignition_period", false)]
        [TestCase(DisabledNotAvailable, "test_model:burner:ignition_settings:ignition_supplying_delay", false)]
        [TestCase(DisabledNotAvailable, "test_model:burner:ignition_settings:ignition_reset_delay", false)]
        [TestCase(DisabledAvailable, "test_model:burner:ignition_settings:ignition_period", false)]
        [TestCase(DisabledAvailable, "test_model:burner:ignition_settings:ignition_supplying_delay", false)]
        [TestCase(DisabledAvailable, "test_model:burner:ignition_settings:ignition_reset_delay", false)]
        public void should_handle_timeout_occured(State currentState, string timerUrn, bool expected)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .WithSettingsUrns(new Urn[] { })
                    .BuildSlaveController();

            var trigger = TimeoutOccured.Create(timerUrn, TimeSpan.Zero, Guid.Empty);

            Check.That(slaveController.CanHandle(trigger)).IsEqualTo(expected);
        }

        private static TestCaseData[] _testCases = new[]
        {
            new TestCaseData(EnabledAvailable, RegulationExited.Create(DeviceNode), true),
            new TestCaseData(EnabledAvailable, RegulationExited.Create(OtherDeviceNode), false),
            new TestCaseData(DisabledAvailable, RegulationExited.Create(DeviceNode), true),
            new TestCaseData(DisabledAvailable, RegulationExited.Create(OtherDeviceNode), false),
            new TestCaseData(EnabledNotAvailable, RegulationEntered.Create(DeviceNode), true),
            new TestCaseData(EnabledNotAvailable, RegulationEntered.Create(OtherDeviceNode), false),
            new TestCaseData(DisabledNotAvailable, RegulationEntered.Create(DeviceNode), true),
            new TestCaseData(DisabledNotAvailable, RegulationEntered.Create(OtherDeviceNode), false)
        };

        [Test, TestCaseSource(nameof(_testCases))]
        public void should_handle_regulation_started_or_exited(State currentState, DomainEvent trigger, bool expected)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .WithSettingsUrns(new Urn[] { })
                    .BuildSlaveController();

            var canHandle = slaveController.CanHandle(trigger);
            Check.That(canHandle).IsEqualTo(expected);
        }

    }
}