using System;
using System.Linq;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.VendorBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.VendorBoard.Controller, ImpliciX.RTUModbus.Controllers.VendorBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;
using SlaveCommunicationError = ImpliciX.Driver.Common.Errors.SlaveCommunicationError;

namespace ImpliciX.RTUModbus.Controllers.Tests.VendorBoard
{
    [TestFixture]
    public class RegulationPeriodicReadTests
    {
        private static microcontroller _microcontroller = test_model.software.fake_other_board._private<microcontroller>();

        [Test]
        public void nominal_read_measures_in_regulation_state()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_other_board)
                        .WithReadPeaceSettings(1)
                        .ReadMainFirmwareSimulation().Returning(ReadProperties).EndSimulation()
                .BuildSlaveController();
                
            var trigger = SystemTicked.Create(1000, 1);
            
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_other_board,TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(slaveController.Group, ReadProperties, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Regulation);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        private static IDataModelValue[] ReadProperties
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

        [Test]
        public void should_read_on_system_ticks_according_to_read_peace_in_defined_in_settings()
        {
             var slaveController = 
                 DefineControllerInState(Regulation)
                     .ForSimulatedSlave(test_model.software.fake_other_board)
                        .WithReadPeaceSettings(2)
                        .ReadMainFirmwareSimulation().Returning(ReadProperties,4).EndSimulation()
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
        public void should_send_communication_occured_error_when_read_fails_with_communication_error()
        {
            var slaveCommunicationError = SlaveCommunicationError.Create(test_model.software.fake_other_board);
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_other_board)
                        .WithReadPeaceSettings(1)
                        .ReadMainFirmwareSimulation().Returning(slaveCommunicationError).EndSimulation()
                    .BuildSlaveController();
            
            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            
            Check.That(resultedEvents.FilterEvents<SlaveCommunicationOccured>()).CountIs(1);
            
            var slaveCommunicationOccured = resultedEvents.FilterEvents<SlaveCommunicationOccured>().First();
            Check.That(slaveCommunicationOccured.CommunicationStatus).IsEqualTo(CommunicationStatus.Error);
        }
    }
}