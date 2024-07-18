using System;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
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
    public class ResetTests
    {
        private static readonly TestBurner GenericBurner = test_model.burner;
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;

        [Test]
        public void should_transitions_to_resetting_when_receives_manual_reset()
        {
            var slaveController =
                DefineControllerInState(Faulted)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._reset).ReturningSuccessResult()
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._stop).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventCommandRequested(GenericBurner._manual_reset, default(NoArg), TimeSpan.Zero);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                NotifyOnTimeoutRequested.Create(GenericBurner.ignition_settings.ignition_reset_delay, TimeSpan.Zero),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (GenericBurner._private<brahma>()._reset.measure, default(NoArg)),
                    (GenericBurner._private<brahma>()._reset.status, MeasureStatus.Success)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Resetting);

            Check.That(resultingEvents)
                .ContainsExactly(expectedEvents);
        }


        [Test]
        public void should_transition_reset_timeout_occurs()
        {
            var slaveController =
                DefineControllerInState(Resetting)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._stop).ReturningSuccessResult()
                    .BuildSlaveController();
            var trigger = TimeoutOccured.Create(GenericBurner.ignition_settings.ignition_reset_delay, TimeSpan.Zero, Guid.Empty);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                NotifyOnTimeoutRequested.Create(GenericBurner.ignition_settings.ignition_reset_delay, TimeSpan.Zero),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (GenericBurner._private<brahma>()._stop.measure, default(NoArg)),
                    (GenericBurner._private<brahma>()._stop.status, MeasureStatus.Success)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(ResetPerformed);

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }
    }
}