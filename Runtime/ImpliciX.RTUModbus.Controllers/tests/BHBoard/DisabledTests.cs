using System;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<
    ImpliciX.RTUModbus.Controllers.BHBoard.Controller, ImpliciX.RTUModbus.Controllers.BHBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class DisabledTests
    {
        private static readonly microcontroller Microcontroller =
            test_model.software.fake_daughter_board._private<microcontroller>();

        private static TestCaseData[] _testCases = new[]
        {
            new TestCaseData(Regulation, new DomainEvent[]
            {
                RegulationExited.Create(test_model.software.fake_daughter_board),
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, TimeSpan.Zero,
                    Zero_CommunicationDetails)
            }),
            new TestCaseData(Initializing, new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, TimeSpan.Zero,
                    Zero_CommunicationDetails)
            }),
        };

        [Test, TestCaseSource(nameof(_testCases))]
        public void should_transition_to_disabled_when_receiving_presence_disabled(State currentState,
            DomainEvent[] expectedEvents)
        {
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();

            var trigger = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Disabled,
                TimeSpan.Zero);

            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            Check.That(resultedEvents).ContainsExactly(expectedEvents);
            Check.That(slaveController.CurrentState).IsEqualTo(Disabled);
        }

        [Test]
        public void should_transition_to_working_when_receiving_presence_enabled()
        {
            var slaveController =
                DefineControllerInState(Disabled)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ExecuteCommandSimulation(Microcontroller.bootloader._switch).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventPropertyChanged(test_model.software.fake_daughter_board.presence, Presence.Enabled,
                TimeSpan.Zero);

            slaveController.HandleDomainEvent(trigger);

            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
        }
    }
}