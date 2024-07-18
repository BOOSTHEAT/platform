using System;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RTUModbus.Controllers.VendorBoard;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.VendorBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.VendorBoard.Controller, ImpliciX.RTUModbus.Controllers.VendorBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.VendorBoard
{
    [TestFixture]
    public class DisabledTests
    {
        private static microcontroller _microcontroller = test_model.software.fake_other_board._private<microcontroller>();
        
        [TestCase(Regulation)]
        public void should_transition_to_disabled_when_receiving_presence_disabled(State currentState)
        {
            var slaveController =DefineControllerInState(currentState)
                                .ForSimulatedSlave(test_model.software.fake_other_board)
                                .BuildSlaveController();

            var trigger = EventPropertyChanged(test_model.software.fake_other_board.presence, Presence.Disabled, TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvent = SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_other_board, TimeSpan.Zero, Zero_CommunicationDetails);
            Check.That(resultedEvents).ContainsExactly(expectedEvent);

            Check.That(slaveController.CurrentState).IsEqualTo(Disabled);
        }
        
       [Test]
        public void should_transition_to_working_when_receiving_presence_enabled()
        {
            var slaveController = DefineControllerInState(Disabled).
                                  ForSimulatedSlave(test_model.software.fake_other_board)
                                    .ExecuteCommandSimulation(_microcontroller.bootloader._switch).ReturningSuccessResult()
                                  .BuildSlaveController();

            var trigger = EventPropertyChanged(test_model.software.fake_other_board.presence, Presence.Enabled, TimeSpan.Zero);

            slaveController.HandleDomainEvent(trigger);

            Check.That(slaveController.CurrentState).IsEqualTo(Regulation);
        }
    }
}