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
    public class IgnitionTests
    {
        private static readonly TestBurner GenericBurner = test_model.burner;
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;

                [Test]
        public void should_transition_to_igniting_when_receives_start_command()
        {
            var slaveController =
                DefineControllerInState(WaitingIgnition)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._start).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventCommandRequested(GenericBurner._start_ignition, default(NoArg), TimeSpan.Zero);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                NotifyOnTimeoutRequested.Create(GenericBurner.ignition_settings.ignition_period, TimeSpan.Zero),
                EventPropertyChanged(
                    TimeSpan.Zero,
                    slaveController.Group,
                    (GenericBurner._private<brahma>()._start.measure, default(NoArg)),
                    (GenericBurner._private<brahma>()._start.status, MeasureStatus.Success)
                ),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Igniting);

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_transitions_to_ignited_when_receives_timeout()
        {
            var slaveController =
                DefineControllerInState(Igniting)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .BuildSlaveController();
            var trigger = TimeoutOccured.Create(GenericBurner.ignition_settings.ignition_period, TimeSpan.Zero, Guid.Empty);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(new (Urn urn, object value)[]
                {
                    (GenericBurner.status, GasBurnerStatus.Ignited),
                }, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Ignited);

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }
    }
}