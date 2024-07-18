using System;
using System.Linq;
using ImpliciX.Driver.Common.Errors;
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
using SlaveCommunicationError = ImpliciX.Driver.Common.Errors.SlaveCommunicationError;

namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class RegulationPeriodicReadTests
    {
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;
        private static readonly microcontroller _microcontroller = DeviceNode._private<microcontroller>();
        private CommunicationDetails Healthy_CommunicationDetails = new CommunicationDetails(1,0);
        private CommunicationDetails Error_CommunicationDetails = new CommunicationDetails(0,1);


        [Test]
        public void protocol_error_occurs_in_regulation_state()
        {
            var bootloaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller.board_state.measure, BoardState.RegulationStarted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.board_state.status, MeasureStatus.Success, TimeSpan.Zero),
            };

            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch)
                        .ReturningSuccessResult()
                    .ReadBootloaderSimulation().Returning(bootloaderProperties).EndSimulation()
                .BuildSlaveController();

            var trigger = ProtocolErrorOccured.Create(DeviceNode);
            slaveController.HandleDomainEvent(trigger);

            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
        } 
        
        [Test]
        public void nominal_read_measures_in_regulation_state()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .WithReadPeaceSettings(1)
                        .ReadMainFirmwareSimulation().Returning(ReadMainFirmwareProperties).EndSimulation()
                .BuildSlaveController();
                
            var trigger = SystemTicked.Create(1000, 1);
            
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode,TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(slaveController.Group, ReadMainFirmwareProperties, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Regulation);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_not_read_bootloader_properties_while_in_regulation_state()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .WithReadPeaceSettings(1)
                    .ReadMainFirmwareSimulation().Returning(ReadMainFirmwareProperties).EndSimulation()
                    .ReadBootloaderSimulation().Returning(ReadBootloaderProperties).EndSimulation()
                    .BuildSlaveController();
                
            var trigger = SystemTicked.Create(1000, 1);
            
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode,TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(slaveController.Group, ReadMainFirmwareProperties, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Regulation);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        
        
        private static IDataModelValue[] ReadMainFirmwareProperties
        {
            get
            {
                return new IDataModelValue[]
                {
                    Property<Temperature>.Create(test_model.measures.temperature1.measure,Temperature.Create(1f),TimeSpan.Zero ),
                    Property<Pressure>.Create(test_model.measures.pressure1.measure,Pressure.FromFloat(1f).Value,TimeSpan.Zero ),
                    Property<MeasureStatus>.Create(test_model.measures.temperature1.status,MeasureStatus.Success,TimeSpan.Zero ),
                    Property<MeasureStatus>.Create(test_model.measures.pressure1.status,MeasureStatus.Success,TimeSpan.Zero)
                };
            }
        }
        
        private static IDataModelValue[] ReadBootloaderProperties
        {
            get
            {
                return new IDataModelValue[]
                {
                    Property<LogicalDeviceError>.Create(DeviceNode.last_error.measure,LogicalDeviceError.HardwareReset,TimeSpan.Zero ),
                    Property<MeasureStatus>.Create(DeviceNode.last_error.status,MeasureStatus.Success,TimeSpan.Zero ),
                };
            }
        }

        [Test]
        public void should_read_on_system_ticks_according_to_read_peace_in_defined_in_settings()
        {
             var slaveController =
                 DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .WithReadPeaceSettings(2)
                        .ReadMainFirmwareSimulation().Returning(ReadMainFirmwareProperties,4).EndSimulation()
                .BuildSlaveController();

             { 
                 var trigger = SystemTicked.Create(1000, 1);
                 var resultedEvents = slaveController.HandleDomainEvent(trigger);
                 Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(0);
             }

             {
                 var trigger = SystemTicked.Create(1000, 2);
                 var resultedEvents = slaveController.HandleDomainEvent(trigger);
                 Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(1);
             }
             
             { 
                 var trigger = SystemTicked.Create(1000, 3);
                 var resultedEvents = slaveController.HandleDomainEvent(trigger);
                 Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(0);
             }
             
             {
                 var trigger = SystemTicked.Create(1000, 4);
                 var resultedEvents = slaveController.HandleDomainEvent(trigger);
                 Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(1);
             }
        }

        [Test]
        public void daughterboard_unexpected_reboot()
        {
            
            var readProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller.board_state.measure,BoardState.WaitingForStart,TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller.board_state.status,MeasureStatus.Success,TimeSpan.Zero),
            };
            
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .WithReadPeaceSettings(1)
                    .ReadMainFirmwareSimulation().Returning(readProperties).EndSimulation()
                .BuildSlaveController();
                
            var trigger = SystemTicked.Create(1000, 1);
            
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                ProtocolErrorOccured.Create(DeviceNode)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        } 
        
        [Test]
        public void daughterboard_unexpected_reboot_with_slave_protocol_error()
        {
            var readProtocolError = ReadProtocolError.Create(DeviceNode);
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .WithReadPeaceSettings(1)
                    .ReadMainFirmwareSimulation()
                        .WithReadProtocolError().EndSimulation()
                .BuildSlaveController();
                
            var trigger = SystemTicked.Create(1000, 1);
            
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(DeviceNode, TimeSpan.Zero, Error_CommunicationDetails),
                ProtocolErrorOccured.Create(DeviceNode)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_send_communication_occured_error_when_read_fails_with_communication_error()
        {
            var slaveCommunicationError = SlaveCommunicationError.Create(DeviceNode);
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(DeviceNode)
                    .WithReadPeaceSettings(1)
                    .ReadMainFirmwareSimulation()
                        .WithSlaveCommunicationError().EndSimulation()
                .BuildSlaveController();
            
            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            
            Check.That(resultedEvents.FilterEvents<SlaveCommunicationOccured>()).CountIs(1);
            
            var slaveCommunicationOccured = resultedEvents.FilterEvents<SlaveCommunicationOccured>().First();
            Check.That(slaveCommunicationOccured.CommunicationStatus).IsEqualTo(CommunicationStatus.Error);
        }
    }
}