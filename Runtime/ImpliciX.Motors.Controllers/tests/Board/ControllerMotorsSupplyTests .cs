using System;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Board.State;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.Motors.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.Motors.Controllers.Board.SlaveController, ImpliciX.Motors.Controllers.Board.State>;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    [TestFixture]
    public class ControllerMotorsSupplyTests 
    {
        [Test]
        public void the_initial_state_should_be_stopped()
        {
            var slaveController = DefineController()
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();

            slaveController.Activate();
            Check.That(slaveController.CurrentState).IsEqualTo(StoppedNominal);
        }
        
        
        [TestCase(StoppedNominal)]
        [TestCase(PowerFailure)]
        public void should_be_starting_on_command_switch_start(State currentState)
        {
            var slaveController = DefineControllerInState(currentState)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();
            
            var trigger = EventCommandRequested(test_model.motors._switch, MotorStates.Start, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            var expected_events = new DomainEvent[]
            {
                EventCommandRequested(test_model.motors._power, PowerSupply.On, TimeSpan.Zero),
                EventCommandRequested(test_model.motors._supply, PowerSupply.On, TimeSpan.Zero),
                EventNotifyOnTimeoutRequested(test_model.motors.supply_delay,TimeSpan.Zero),
                EventPropertyChanged(test_model.motors.status, Starting, TimeSpan.Zero)
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Starting);
            Check.That(resultedEvents).ContainsExactly(expected_events);
        }
        
        [TestCase(Starting)]
        [TestCase(Started)]
        public void should_ignore_start_command_when_already_started_or_starting(State state)
        {
            var slaveController = DefineControllerInState(state)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();
            
            var trigger = EventCommandRequested(test_model.motors._switch, MotorStates.Start, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            Check.That(slaveController.CurrentState).IsEqualTo(state);
            Check.That(resultedEvents).IsEmpty();
        }

        [Test]
        public void should_be_started_after_supply_delay_timeout()
        {
            var slaveController = DefineControllerInState(Starting)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();
            
            var trigger = EventTimeoutOccured(test_model.motors.supply_delay,TimeSpan.Zero);
            var _ = slaveController.HandleDomainEvent(trigger);
            Check.That(slaveController.CurrentState).IsEqualTo(Started);
        }
        
        [TestCase(Starting)]
        [TestCase(Started)]
        public void should_be_stopped_on_commmand_switch_stop(State currentState)
        {
            var slaveController = DefineControllerInState(currentState)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();
            
            var trigger = EventCommandRequested(test_model.motors._switch, MotorStates.Stop, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, TimeSpan.Zero, new CommunicationDetails(0,0)),
                EventCommandRequested(test_model.motors._supply, PowerSupply.Off, TimeSpan.Zero),
                EventCommandRequested(test_model.motors._power, PowerSupply.Off, TimeSpan.Zero),
                EventPropertyChanged(test_model.motors.status, MotorsStatus.Stopped, TimeSpan.Zero),
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [TestCase(Starting)]
        [TestCase(Started)]
        public void should_be_stopped_when_heat_pump_restarts(State currentState)
        {
            var slaveController = DefineControllerInState(currentState)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();

            var trigger = SlaveRestarted.Create(test_model.software.fake_heat_pump, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, TimeSpan.Zero, new CommunicationDetails(0,0)), 
                EventPropertyChanged(test_model.motors.status, MotorsStatus.Stopped, TimeSpan.Zero),
               
            };
            Check.That(slaveController.CurrentState).IsEqualTo(PowerFailure);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void should_not_be_stopped_when_mcu_other_than_heat_pump_restarts()
        {
            var slaveController = DefineControllerInState(Started)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .BuildSlaveController();
            
            var trigger = SlaveRestarted.Create(test_model.software.fake_eu, TimeSpan.Zero);
            slaveController.HandleDomainEvent(trigger);
           
            Check.That(slaveController.CurrentState).IsEqualTo(Started);
        }
    }
}