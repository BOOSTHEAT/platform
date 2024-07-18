using System;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.Motors.Controllers.Board.SlaveController, ImpliciX.Motors.Controllers.Board.State>;
using static ImpliciX.Motors.Controllers.Board.State;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    [TestFixture]
    public class ControllerExecuteCommandsTests
    {
        private CommunicationDetails Healthy_CommunicationDetails  = new CommunicationDetails(1,0);
        private CommunicationDetails Error_CommunicationDetails =    new CommunicationDetails(0,1);


        [Test]
        public void should_send_command_success_properties_when_command_execution_succeeds()
        {
            var slaveController =
                DefineControllerInState(Started)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .ExecuteCommandSimulation(test_model.motors._1._setpoint).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = CommandRequested.Create(test_model.motors._1._setpoint.command, RotationalSpeed.FromFloat(32f).Value, TimeSpan.Zero);

            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (test_model.motors._1._setpoint.measure, RotationalSpeed.FromFloat(32f).Value),
                    (test_model.motors._1._setpoint.status, MeasureStatus.Success)),
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);

        }
        
        [Test]
        public void should_send_fatal_communication_error_on_command_failure()
        {
            var slaveController = DefineControllerInState(Started)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .ExecuteCommandSimulation(test_model.motors._1._setpoint).WithExecutionError()
                .BuildSlaveController();

            var trigger = EventCommandRequested(test_model.motors._1._setpoint.command, RotationalSpeed.FromFloat(32f).Value, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(test_model.software.fake_motor_board, TimeSpan.Zero, Error_CommunicationDetails), 
                EventPropertyChanged(test_model.motors._1._setpoint.status, MeasureStatus.Failure, TimeSpan.Zero),
            };
            Check.That(resultedEvents).Contains(expectedEvents);
        }

        [TestCase(Starting)]
        [TestCase(StoppedNominal)]
        [TestCase(PowerFailure)]
        public void should_ignore_command_requests_and_not_send_communication_fatal_when_not_started(State currentState)
        {
            var slaveController = DefineControllerInState(currentState)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .ExecuteCommandSimulation(test_model.motors._1._setpoint).ReturningSuccessResult()
                .BuildSlaveController();
            
            var trigger = EventCommandRequested(test_model.motors._1._setpoint.command, RotationalSpeed.FromFloat(32f).Value, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            Check.That(resultedEvents).IsEmpty();
        }
    }
}