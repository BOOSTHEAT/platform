using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BrahmaBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<
    ImpliciX.RTUModbus.Controllers.BrahmaBoard.Controller,
    ImpliciX.RTUModbus.Controllers.BrahmaBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.BrahmaBoard
{
    [TestFixture]
    public class ReadTests
    {
        private static readonly TestBurner GenericBurner = test_model.burner;
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;

        [Test]
        public void when_enabled_and_available_should_read_properties_on_system_ticked()
        {
            var propsT1 = new IDataModelValue[]
            {
                Property<Fault>.Create(GenericBurner.ignition_fault.measure, Fault.Faulted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(GenericBurner.ignition_fault.status, MeasureStatus.Success,
                    TimeSpan.Zero),
            };
            var slaveController =
                DefineControllerInState(EnabledAvailable)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ReadMainFirmwareSimulation().Returning(propsT1).EndSimulation()
                    .BuildSlaveController();
            var trigger = EventSystemTicked(1000, TimeSpan.Zero);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (GenericBurner.ignition_fault.measure, Fault.Faulted),
                    (GenericBurner.ignition_fault.status, MeasureStatus.Success)),
            };

            Check.That(resultingEvents).Contains(expectedEvents);
        }
        
        [TestCase(EnabledNotAvailable)]
        [TestCase(DisabledNotAvailable)]
        [TestCase(DisabledAvailable)]
        public void when_in_other_top_level_states_should_not_read_properties_on_system_ticked(State state)
        {
            var propsT1 = new IDataModelValue[]
            {
                Property<Fault>.Create(GenericBurner.ignition_fault.measure, Fault.Faulted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(GenericBurner.ignition_fault.status, MeasureStatus.Success,
                    TimeSpan.Zero),
            };
            var slaveController =
                DefineControllerInState(state)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ReadMainFirmwareSimulation().Returning(propsT1).ThenReturning(propsT1).EndSimulation()
                    .BuildSlaveController();
            var trigger = EventSystemTicked(1000, TimeSpan.Zero);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = Array.Empty<DomainEvent>();

            Check.That(resultingEvents).Contains(expectedEvents);
        }
    }
}