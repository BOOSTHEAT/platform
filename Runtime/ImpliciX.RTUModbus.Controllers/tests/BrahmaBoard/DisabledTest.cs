using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.BrahmaBoard.Controller, ImpliciX.RTUModbus.Controllers.BrahmaBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.BrahmaBoard
{
    public class DisabledTest
    {
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;
        private static readonly TestBurner GenericBurner = test_model.burner;

        [Test]
        public void should_round_trip_to_standby_disabled_first()
        {
            var slaveController =
                DefineControllerInState(StandBy)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .BuildSlaveController();

            var propertyChanged = EventPropertyChanged(DeviceNode.presence, Presence.Disabled, TimeSpan.Zero);
            slaveController.HandleDomainEvent(propertyChanged);
            Check.That(slaveController.CurrentState).IsEqualTo(DisabledAvailable);
            
            var regulationExited = RegulationExited.Create(test_model.software.fake_daughter_board);
            slaveController.HandleDomainEvent(regulationExited);
            Check.That(slaveController.CurrentState).IsEqualTo(DisabledNotAvailable);

            propertyChanged = EventPropertyChanged(DeviceNode.presence, Presence.Enabled, TimeSpan.Zero);
            slaveController.HandleDomainEvent(propertyChanged);
            Check.That(slaveController.CurrentState).IsEqualTo(EnabledNotAvailable);
        
            var regulationEntered = RegulationEntered.Create(test_model.software.fake_daughter_board);
            slaveController.HandleDomainEvent(regulationEntered);
            Check.That(slaveController.CurrentState).IsEqualTo(StandBy);
        }
        [Test]

        public void should_round_trip_to_standby_update_first()
        {
            var slaveController =
                DefineControllerInState(StandBy)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .BuildSlaveController();

            var regulationExited = RegulationExited.Create(test_model.software.fake_daughter_board);
            slaveController.HandleDomainEvent(regulationExited);
            Check.That(slaveController.CurrentState).IsEqualTo(EnabledNotAvailable);

            var propertyChanged = EventPropertyChanged(DeviceNode.presence, Presence.Disabled, TimeSpan.Zero);
            slaveController.HandleDomainEvent(propertyChanged);
            Check.That(slaveController.CurrentState).IsEqualTo(DisabledNotAvailable);
            
            var regulationEntered = RegulationEntered.Create(test_model.software.fake_daughter_board);
            slaveController.HandleDomainEvent(regulationEntered);
            Check.That(slaveController.CurrentState).IsEqualTo(DisabledAvailable);

            propertyChanged = EventPropertyChanged(DeviceNode.presence, Presence.Enabled, TimeSpan.Zero);
            slaveController.HandleDomainEvent(propertyChanged);
            Check.That(slaveController.CurrentState).IsEqualTo(StandBy);
        }
    }
}