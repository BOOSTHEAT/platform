using System;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Board.State;
using static ImpliciX.Motors.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.Motors.Controllers.Board.SlaveController, ImpliciX.Motors.Controllers.Board.State>;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    [TestFixture]
    public class ControllerPresenceTests
    {
        [Test]
        public void should_transition_to_disabled_state_when_presence_disabled()
        {
            var slaveController =
                DefineControllerInState(Enabled)
                    .WithSettingsUrns(new Urn[]
                    {
                        test_model.software.fake_motor_board.presence
                    })
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .BuildSlaveController();

            var trigger = PropertiesChanged.Create(new IDataModelValue[]
            {
                Property<Presence>.Create(test_model.software.fake_motor_board.presence, Presence.Disabled, TimeSpan.Zero),
            }, TimeSpan.Zero);

            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            Check.That(slaveController.CurrentState).IsEqualTo(Disabled);
            
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, TimeSpan.Zero, new CommunicationDetails(0,0)),
                EventCommandRequested(test_model.motors._supply, PowerSupply.Off, TimeSpan.Zero),
                EventCommandRequested(test_model.motors._power, PowerSupply.Off, TimeSpan.Zero),
                EventPropertyChanged(test_model.motors.status, MotorsStatus.Stopped, TimeSpan.Zero),
            };
            
            Check.That(resultedEvents).ContainsExactly(expectedEvents);        }
        
        [Test]
        public void should_transition_to_enabled_state_when_presence_enabled()
        {
            var slaveController =
                DefineControllerInState(Disabled)
                    .WithSettingsUrns(new Urn[]
                    {
                        test_model.software.fake_motor_board.presence
                    })
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .BuildSlaveController();

            var trigger = PropertiesChanged.Create(new IDataModelValue[]
            {
                Property<Presence>.Create(test_model.software.fake_motor_board.presence, Presence.Enabled, TimeSpan.Zero),
            }, TimeSpan.Zero);

            slaveController.HandleDomainEvent(trigger);

            Check.That(slaveController.CurrentState).IsEqualTo(StoppedNominal);
        }
    }
}