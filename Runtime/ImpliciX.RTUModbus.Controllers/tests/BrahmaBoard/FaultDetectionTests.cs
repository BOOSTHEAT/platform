using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BrahmaBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.BrahmaBoard.Controller,
    ImpliciX.RTUModbus.Controllers.BrahmaBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.BrahmaBoard
{
    [TestFixture]
    public class FaultDetectionTests
    {
        private static readonly TestBurner GenericBurner = test_model.burner;
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;

        [TestCase(WaitingIgnition)]
        [TestCase(Igniting)]
        [TestCase(Ignited)]
        [TestCase(CheckReadiness)]
        public void should_transition_to_faulted_when_fault_status_detected(State currentState)
        {
            var props_t1 = new IDataModelValue[]
            {
                Property<Fault>.Create(GenericBurner.ignition_fault.measure, Fault.Faulted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(GenericBurner.ignition_fault.status, MeasureStatus.Success, TimeSpan.Zero),
            };
            var slaveController =
                DefineControllerInState(currentState)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ReadMainFirmwareSimulation().Returning(props_t1).ThenReturning(props_t1).EndSimulation()
                    .BuildSlaveController();
            var trigger = EventSystemTicked(1000, TimeSpan.Zero);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                FaultedDetected.Create(DeviceNode, GenericBurner)
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }
    }
}