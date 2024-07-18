using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Doubles;
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
    public class CanHandleTests
    {
        [TestCase(Regulation)]
        [TestCase(Initializing)]
        [TestCase(Disabled)]
        public void should_not_handle_not_supported_commands(State currentState)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ExecuteCommandSimulation(test_model.commands.do_something)
                        .ReturningSuccessResult()
                .BuildSlaveController();
            
            var trigger = CommandRequested.Create(CommandUrn<Percentage>.Build("Driver command not supported"), Percentage.FromFloat(0.2f).Value, TimeSpan.Zero);
            Check.That(slaveController.CanHandle(trigger)).IsFalse();
        }
        
        
        [Test]
        public void should_handle_supported_commands_when_in_state_regulation()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ExecuteCommandSimulation(test_model.commands.do_something)
                        .ReturningSuccessResult()
                .BuildSlaveController();
            
            var trigger = CommandRequested.Create(test_model.commands.do_something.command, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero);
            Check.That(slaveController.CanHandle(trigger)).IsTrue();
        }
        
        [TestCase(Regulation, true)]
        [TestCase(Updating, true)]
        [TestCase(UpdateInitializing, true)]
        [TestCase(UpdateStarting, true)]
        [TestCase(WaitingUploadReady, true)]
        [TestCase(Uploading, true)]
        [TestCase(UploadCompleted, true)]
        public void can_handle_system_ticked(State currentState, bool expected)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();
            
            var trigger = SystemTicked.Create(1000, 1);
            Check.That(slaveController.CanHandle(trigger)).IsEqualTo(expected);
        }
        
        
        [TestCase(typeof(SystemTicked), Disabled)]
        [TestCase(typeof(CommandRequested), Disabled)]
        [TestCase(typeof(PropertiesChanged), Disabled)]
        [TestCase(typeof(PropertiesChanged), Regulation)]
        public void should_not_handle_events_when_disabled(Type triggerType, State currentState)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();

            var triggers = new Dictionary<Type, DomainEvent>()
            {
                [typeof(SystemTicked)] = SystemTicked.Create(1000, 1),
                [typeof(CommandRequested)] = CommandRequested.Create(test_model.commands.do_something.command,Percentage.FromFloat(0f).Value, TimeSpan.Zero),
                [typeof(PropertiesChanged)] = PropertiesChanged.Create(test_model.measures.temperature1.measure,Temperature.Create(12), TimeSpan.Zero)
            };  
            
            var result = slaveController.CanHandle(triggers[triggerType]);
            Check.That(result).IsFalse();
        }


        [TestCase(Disabled)]
        [TestCase(Initializing)]
        [TestCase(Regulation)]
        public void should_handle_PropertiesChanged_containing_slave_settings_urns(State currentState)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithSettingsUrns(new Urn[]
                    {
                        test_model.software.fake_daughter_board.presence,
                        test_model.measures.pressure1.measure
                    })
                    .BuildSlaveController();
            var presenceEnabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Enabled, TimeSpan.Zero);
            var presenceDisabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Disabled, TimeSpan.Zero);
            var pressureMeasure = EventPropertyChanged(test_model.measures.pressure1.measure, 2f, TimeSpan.Zero);
            Check.That(slaveController.CanHandle(presenceEnabled)).IsTrue();
            Check.That(slaveController.CanHandle(presenceDisabled)).IsTrue();
            Check.That(slaveController.CanHandle(pressureMeasure)).IsTrue();
        }
        
        [Test]
        public void should_not_handle_PropertiesChanged_not_containing_slave_settings_urns()
        {
            var slaveController =
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithSettingsUrns(new Urn[] {}) 
                    .BuildSlaveController();
            var pressureMeasure = EventPropertyChanged(test_model.measures.pressure1.measure, 2f, TimeSpan.Zero);
            var presenceEnabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Enabled, TimeSpan.Zero);
            var presenceDisabled = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Disabled, TimeSpan.Zero); 
            Check.That(slaveController.CanHandle(pressureMeasure)).IsFalse();
            Check.That(slaveController.CanHandle(presenceDisabled)).IsFalse();
            Check.That(slaveController.CanHandle(presenceEnabled)).IsFalse();
        }

        [TestCase(Disabled, true)]
        [TestCase(UploadCompleted, true)]
        public void handle_CommitCommands(State currentState, bool expected)
        {
            var slaveController = 
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();
            var trigger = EventCommandRequested(test_model._commit_update, default(NoArg), TimeSpan.Zero);
            Check.That(slaveController.CanHandle(trigger)).Equals(expected);
        }
        
        [TestCase(Disabled, true)]
        [TestCase(UpdateInitializing, true)]
        [TestCase(UpdateStarting, true)]
        [TestCase(WaitingUploadReady, true)]
        [TestCase(Uploading, true)]
        [TestCase(UploadCompleted, true)]
        public void handle_RollbackCommands(State currentState, bool expected)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();
            var trigger = EventCommandRequested(test_model._rollback_update, default(NoArg), TimeSpan.Zero);
            Check.That(slaveController.CanHandle(trigger)).Equals(expected);
        }

        [TestCase(Disabled, false)]
        [TestCase( Regulation, true)]
        [TestCase(Initializing, true)]
        [TestCase(Regulation, true)]
        [TestCase(UpdateInitializing, true)]
        [TestCase(UpdateStarting, true)]
        [TestCase(WaitingUploadReady, true)]
        [TestCase(Uploading, true)]
        [TestCase(UploadCompleted, true)]
        public void should_handle_private_domain_events_when_the_softwaredevice_is_the_same_as_the_controlled_slave(State currentState, bool expected)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();
            var trigger = new DummyPrivateDomainEvent(test_model.software.fake_daughter_board);
            Check.That(slaveController.CanHandle(trigger)).Equals(expected);
        }
        
        
        [TestCase(Disabled)]
        [TestCase(Regulation)]
        [TestCase(Initializing)]
        [TestCase(Regulation)]
        [TestCase(UpdateInitializing)]
        [TestCase(UpdateStarting)]
        [TestCase(WaitingUploadReady)]
        [TestCase(Uploading)]
        [TestCase(UploadCompleted)]
        public void should_not_handle_private_domain_events_when_the_softwaredevice_is_not_the_same_as_the_controlled_slave(State currentState)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();
            var trigger = new DummyPrivateDomainEvent(test_model.software.fake_other_board);
            Check.That(slaveController.CanHandle(trigger)).IsFalse();
        }
    }
}