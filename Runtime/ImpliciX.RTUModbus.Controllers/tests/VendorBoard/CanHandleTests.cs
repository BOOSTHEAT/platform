using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RTUModbus.Controllers.VendorBoard;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.VendorBoard.Controller, ImpliciX.RTUModbus.Controllers.VendorBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.RTUModbus.Controllers.VendorBoard.State;

namespace ImpliciX.RTUModbus.Controllers.Tests.VendorBoard
{

    [TestFixture]
    public class CanHandleTests
    {
        [TestCase(typeof(SystemTicked), Disabled)]
        [TestCase(typeof(PropertiesChanged), Disabled)]
        [TestCase(typeof(PropertiesChanged), Regulation)]
        public void should_not_handle_events_when_disabled(Type triggerType, State currentState)
        {
            var slaveController = DefineControllerInState(currentState).
                                  ForSimulatedSlave(test_model.software.fake_other_board)
                                  .BuildSlaveController();

            var triggers = new Dictionary<Type, DomainEvent>()
            {
                [typeof(SystemTicked)] = SystemTicked.Create(1000, 1),
                [typeof(PropertiesChanged)] = PropertiesChanged.Create(test_model.measures.temperature1.measure,Temperature.Create(12), TimeSpan.Zero)
            };  
                
            var result = slaveController.CanHandle(triggers[triggerType]);
            Check.That(result).IsFalse();
        }        
        
        [TestCase(Regulation, true)]
        public void can_handle_system_ticked(State currentState, bool expected)
        {
            var slaveController = DefineControllerInState(currentState).
                                  ForSimulatedSlave(test_model.software.fake_other_board)
                                  .BuildSlaveController();
            var trigger = SystemTicked.Create(1000, 1);
            Check.That(slaveController.CanHandle(trigger)).IsEqualTo(expected);
        }
        
        [TestCase(Disabled)]
        [TestCase(Regulation)]
        public void should_handle_PropertiesChanged_containing_slave_settings_urns(State currentState)
        {
            var slaveController = DefineControllerInState(currentState).
                                  ForSimulatedSlave(test_model.software.fake_other_board)
                                  .WithSettingsUrns(new Urn[]
                                  {
                                      test_model.software.fake_other_board.presence
                                  })
                                  .BuildSlaveController();
            
            var presenceEnabled = EventPropertyChanged(test_model.software.fake_other_board.presence, Presence.Enabled, TimeSpan.Zero);
            var presenceDisabled = EventPropertyChanged(test_model.software.fake_other_board.presence, Presence.Disabled, TimeSpan.Zero);
            Check.That(slaveController.CanHandle(presenceEnabled)).IsTrue();
            Check.That(slaveController.CanHandle(presenceDisabled)).IsTrue();
        }
        
    }
}