using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.BHBoard.Controller, ImpliciX.RTUModbus.Controllers.BHBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class RegulationExecuteCommandsTests
    {
        private CommunicationDetails Healthy_CommunicationDetails = new CommunicationDetails(1,0);
        private CommunicationDetails Error_CommunicationDetails = new CommunicationDetails(0,1);

        
        [Test]
        public void should_execute_command_requested_in_regulation()
        {
            var commandResultProperties = new IDataModelValue[]
            {
                Property<Percentage>.Create(test_model.commands.do_something.measure, Percentage.FromFloat(.1f).Value, TimeSpan.Zero),
                Property<MeasureStatus>.Create(test_model.commands.do_something.status, MeasureStatus.Success, TimeSpan.Zero)
            };
            
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ExecuteCommandSimulation(test_model.commands.do_something).ReturningResult(commandResultProperties)
                .BuildSlaveController();

            var trigger = CommandRequested.Create(test_model.commands.do_something.command, Percentage.FromFloat(.1f).Value,TimeSpan.Zero);
           
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(slaveController.Group, commandResultProperties, TimeSpan.Zero),
            };
                
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_send_communication_occured_fatal_and_a_property_changed_indicating_the_status_of_the_command_when_execution_fails()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ExecuteCommandSimulation(test_model.commands.do_something)
                    .WithExecutionError()
                .BuildSlaveController();

            var trigger = CommandRequested.Create(test_model.commands.do_something.command, Percentage.FromFloat(.1f).Value,TimeSpan.Zero);
           
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(test_model.software.fake_daughter_board, TimeSpan.Zero, Error_CommunicationDetails), 
                EventPropertyChanged(test_model.commands.do_something.status, MeasureStatus.Failure, TimeSpan.Zero),
            };
            
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
    }
}