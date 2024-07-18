using System;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.Motors.Controllers.Board.SlaveController, ImpliciX.Motors.Controllers.Board.State>;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    [TestFixture]
    public class CanHandleTests
    {
        [TestCase(State.Enabled)]
        [TestCase(State.Starting)]
        [TestCase(State.Started)]
        [TestCase(State.StoppedNominal)]
        public void should_not_handle_not_supported_commands(State currentState)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .ExecuteCommandSimulation(test_model.motors._1._setpoint).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventCommandRequested(CommandUrn<NoArg>.Build("whatever"),default, TimeSpan.Zero);

            Check.That(slaveController.CanHandle(trigger)).IsFalse();
        }

        [TestCase(State.Enabled)]
        [TestCase(State.Starting)]
        [TestCase(State.Started)]
        [TestCase(State.StoppedNominal)]
        public void should_handle_supported_commands_when_in_state_started(State currentState)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .ExecuteCommandSimulation(test_model.motors._1._setpoint).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventCommandRequested(test_model.motors._1._setpoint,RotationalSpeed.FromFloat(0).Value, TimeSpan.Zero);

            Check.That(slaveController.CanHandle(trigger)).IsTrue();
        }
        
        [TestCase(State.Enabled, false)]
        [TestCase(State.Starting, false)]
        [TestCase(State.Started, true)]
        [TestCase(State.StoppedNominal, false)]
        public void can_handle_system_ticked(State currentState, bool expected)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .BuildSlaveController();
            
            var trigger = SystemTicked.Create(1000, 1);
            Check.That(slaveController.CanHandle(trigger)).IsEqualTo(expected);
        }

        [TestCase(State.Enabled, false)]
        [TestCase(State.Starting, true)]
        [TestCase(State.Started, false)]
        [TestCase(State.StoppedNominal, false)]
        public void can_handle_timeout_occured(State currentState, bool expected)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .BuildSlaveController();

            var trigger = EventTimeoutOccured(test_model.motors.supply_delay, TimeSpan.Zero);

            Check.That(slaveController.CanHandle(trigger)).IsEqualTo(expected);
        }

        [TestCase(State.Enabled)]
        [TestCase(State.Starting)]
        [TestCase(State.Started)]
        [TestCase(State.StoppedNominal)]
        public void should_handle_supply_switch_commands(State currentState)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .BuildSlaveController();

            var triggers = new[]
            {
                EventCommandRequested(test_model.motors._switch, MotorStates.Start, TimeSpan.Zero),
                EventCommandRequested(test_model.motors._switch, MotorStates.Stop, TimeSpan.Zero),
            };
            foreach (var trigger in triggers)
            {
                Check.That(slaveController.CanHandle(trigger)).IsTrue();
            }
        }

        [TestCase(State.Starting)]
        [TestCase(State.Started)]
        [TestCase(State.StoppedNominal)]
        [TestCase(State.PowerFailure)]
        public void should_handle_slave_restarted(State currentState)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .BuildSlaveController();

            var trigger = SlaveRestarted.Create(test_model.software.fake_heat_pump, TimeSpan.Zero);

            Check.That(slaveController.CanHandle(trigger)).IsTrue();
        }

        [TestCase(State.Starting)]
        [TestCase(State.Started)]
        [TestCase(State.StoppedNominal)]
        [TestCase(State.PowerFailure)]
        public void should_handle_properties_changed_matching_slave_settings_urn(State currentState)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .WithSettingsUrns(new Urn[]
                    {
                       test_model.software.fake_motor_board.software_version.measure
                    })
                    .BuildSlaveController();

            var softwareVersion= EventPropertyChanged(test_model.software.fake_motor_board.software_version.measure, "1.2.3.4", TimeSpan.Zero);
            var pressureMeasure = EventPropertyChanged(test_model.measures.pressure1.measure, 2f, TimeSpan.Zero);

            Check.That(slaveController.CanHandle(softwareVersion)).IsTrue();            
            Check.That(slaveController.CanHandle(pressureMeasure)).IsFalse();            
        }

        [Test]
        public void should_handle_only_property_changed_matching_slave_settings_urn_when_disabled()
        {
            var slaveController =
                DefineControllerInState(State.Disabled)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .WithSettingsUrns(new Urn[]
                    {
                        test_model.software.fake_motor_board.software_version.measure
                    })
                    .BuildSlaveController();
            
            var triggers = new DomainEvent[]
            {
                EventCommandRequested(test_model.motors._switch, MotorStates.Start, TimeSpan.Zero),
                EventCommandRequested(test_model.motors._switch, MotorStates.Stop, TimeSpan.Zero),
                EventCommandRequested(test_model.motors._1._setpoint,RotationalSpeed.FromFloat(0).Value, TimeSpan.Zero),
                EventTimeoutOccured(test_model.motors.supply_delay, TimeSpan.Zero),
                SystemTicked.Create(1000, 1),
                SlaveRestarted.Create(test_model.software.fake_heat_pump, TimeSpan.Zero)
            };
            
            foreach (var trigger in triggers)
            {
                Check.That(slaveController.CanHandle(trigger)).IsFalse();
            }            
            
            var softwareVersion= EventPropertyChanged(test_model.software.fake_motor_board.software_version.measure, "1.2.3.4", TimeSpan.Zero);

            Check.That(slaveController.CanHandle(softwareVersion)).IsTrue();
        }
    }
}