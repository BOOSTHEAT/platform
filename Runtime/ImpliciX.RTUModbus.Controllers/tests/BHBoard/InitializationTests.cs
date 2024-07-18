using System;
using System.Linq;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.BHBoard.Controller, ImpliciX.RTUModbus.Controllers.BHBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using SlaveCommunicationError = ImpliciX.Driver.Common.Errors.SlaveCommunicationError;

namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class InitializationTests
    {
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;
        private static readonly microcontroller _microcontroller = DeviceNode._private<microcontroller>();
 

        [Test]
        public void initialize_nominal_case()
        {
            var commandResultProperties = new IDataModelValue[]
            {
                Property<MCUBootloader>.Create(_microcontroller.bootloader._switch.measure, MCUBootloader.Exit, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.bootloader._switch.status, MeasureStatus.Success, TimeSpan.Zero)
            };

            var bootloaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller.board_state.measure, BoardState.RegulationStarted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.board_state.status, MeasureStatus.Success, TimeSpan.Zero),
            };


            var slaveController =
                DefineController()
                    .ForSimulatedSlave(DeviceNode)
                    .ReadBootloaderSimulation().Returning(bootloaderProperties).EndSimulation()
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch).ReturningResult(commandResultProperties)
                    .BuildSlaveController();

            var resultedEvents = slaveController.Activate();

            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(bootloaderProperties.Concat(commandResultProperties).ToArray(), TimeSpan.Zero),
                ExitBootloaderCommandSucceeded.Create(DeviceNode),
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, new CommunicationDetails(2,0)),
                SlaveRestarted.Create(DeviceNode, TimeSpan.Zero)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
         }

        [Test]
        public void initialization_fails_when_reading_bootloader_state()
        {
            var commandResultProperties = new IDataModelValue[]
            {
                Property<MCUBootloader>.Create(_microcontroller.bootloader._switch.measure, MCUBootloader.Exit, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.bootloader._switch.status, MeasureStatus.Success, TimeSpan.Zero)
            };

            var slaveCommunicationError = SlaveCommunicationError.Create(DeviceNode);
            var slaveController =
                DefineController()
                    .ForSimulatedSlave(DeviceNode)
                        .ReadBootloaderSimulation().Returning(slaveCommunicationError).EndSimulation()
                        .ExecuteCommandSimulation(_microcontroller.bootloader._switch).ReturningResult(commandResultProperties)
                    .BuildSlaveController();
            
             var resultedEvents = slaveController.Activate();
             Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
             var expectedEvents = new DomainEvent[]
             {
                 SlaveCommunicationOccured.CreateFatal(DeviceNode, TimeSpan.Zero, Error_CommunicationDetails)
             };
             
             Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void initialization_fails_when_sending_exit_bootloader_command()
        {
            var bootloaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller.board_state.measure, BoardState.RegulationStarted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.board_state.status, MeasureStatus.Success, TimeSpan.Zero),
            };


            var slaveController =
                DefineController()
                    .ForSimulatedSlave(DeviceNode)
                    .ReadBootloaderSimulation().Returning(bootloaderProperties).EndSimulation()
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch)
                    .WithExecutionError()
                    .BuildSlaveController();

            var resultedEvents = slaveController.Activate();

            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(DeviceNode, TimeSpan.Zero, Error_CommunicationDetails)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void initialization_succeeds_after_unsuccessful_attempt()
        {
            var commandResultProperties = new IDataModelValue[]
            {
                Property<MCUBootloader>.Create(_microcontroller.bootloader._switch.measure, MCUBootloader.Exit, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.bootloader._switch.status, MeasureStatus.Success, TimeSpan.Zero)
            };

            var bootloaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller.board_state.measure, BoardState.RegulationStarted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.board_state.status, MeasureStatus.Success, TimeSpan.Zero),
            };


            var slaveController =
                DefineControllerInState(Initializing)
                    .ForSimulatedSlave(DeviceNode)
                    .ReadBootloaderSimulation().Returning(bootloaderProperties).EndSimulation()
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch)
                    .ReturningResult(commandResultProperties)
                    .BuildSlaveController();

            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
            var expectedEvents = new DomainEvent[]
            {
                ExitBootloaderCommandSucceeded.Create(DeviceNode),
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, new CommunicationDetails(2,0)),
                SlaveRestarted.Create(DeviceNode, TimeSpan.Zero),
                EventPropertyChanged(new IDataModelValue[]
                {
                    Property<BoardState>.Create(_microcontroller.board_state.measure, BoardState.RegulationStarted, TimeSpan.Zero),
                    Property<MeasureStatus>.Create(_microcontroller.board_state.status, MeasureStatus.Success, TimeSpan.Zero),
                    Property<MCUBootloader>.Create(_microcontroller.bootloader._switch.measure, MCUBootloader.Exit, TimeSpan.Zero),
                    Property<MeasureStatus>.Create(_microcontroller.bootloader._switch.status, MeasureStatus.Success, TimeSpan.Zero),
                }, TimeSpan.Zero),
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void initialization_fails_again()
        {
            var slaveCommunicationError = SlaveCommunicationError.Create(DeviceNode);
            var slaveController =
                DefineControllerInState(Initializing)
                    .ForSimulatedSlave(DeviceNode)
                    .ReadBootloaderSimulation().Returning(slaveCommunicationError).EndSimulation()
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch)
                    .ReturningSuccessResult()
                    .BuildSlaveController();
            
            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            
            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(DeviceNode, TimeSpan.Zero, Error_CommunicationDetails)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void commands_are_refused_while_at_initializing_state()
        {
            var bootloaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller.board_state.measure, BoardState.RegulationStarted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.board_state.status, MeasureStatus.Success, TimeSpan.Zero),
            };
            
            var slaveController =
                DefineControllerInState(Initializing)
                    .ForSimulatedSlave(DeviceNode)
                    .ReadBootloaderSimulation().Returning(bootloaderProperties).EndSimulation()
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventCommandRequested(test_model.commands.do_something.command, Percentage.FromFloat(.1f).Value,
                TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            
            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);

            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(DeviceNode, TimeSpan.Zero,Zero_CommunicationDetails)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void exit_bootloader_succeeds()
        {
            var slaveController =
                DefineControllerInState(Initializing)
                    .ForSimulatedSlave(DeviceNode)
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = ExitBootloaderCommandSucceeded.Create(DeviceNode);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            
            Check.That(slaveController.CurrentState).IsEqualTo(Regulation);
        }
    }
}