using System;
using ImpliciX.Language.Modbus;
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
    public class SupplyingTests
    {
        private static readonly TestBurner GenericBurner = test_model.burner;
        private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;

        [Test]
        public void should_switch_supply_power_supply_on_when_in_state_standby()
        {
            var slaveController =
                DefineControllerInState(StandBy)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._power).ReturningSuccessResult()
                    .BuildSlaveController();

            {
                var trigger = EventCommandRequested(GenericBurner._supply, PowerSupply.On, TimeSpan.Zero);

                var resultingEvents = slaveController.HandleDomainEvent(trigger);
                var expectedEvents = new DomainEvent[]
                {
                    NotifyOnTimeoutRequested.Create(GenericBurner.ignition_settings.ignition_supplying_delay, TimeSpan.Zero),
                    SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                    EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                        (GenericBurner._private<brahma>()._power.measure, PowerSupply.On),
                        (GenericBurner._private<brahma>()._power.status, MeasureStatus.Success)),
                };
                Check.That(slaveController.CurrentState).IsEqualTo(Supplying);

                Check.That(resultingEvents)
                    .ContainsExactly(expectedEvents);
            }
        }

        [Test]
        public void should_switch_from_supplying_when_ignition_supplying_delay_timeout()
        {
            var slaveController =
                DefineControllerInState(Supplying)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._reset).ReturningSuccessResult()
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._stop).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventTimeoutOccured(GenericBurner.ignition_settings.ignition_supplying_delay, TimeSpan.Zero);

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
        [Ignore("What is this scenario?")]
        public void should_detect_burner_not_faulted()
        {
            var props_t1 = new IDataModelValue[]
            {
                Property<Fault>.Create(GenericBurner.ignition_fault.measure, Fault.Faulted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(GenericBurner.ignition_fault.status, MeasureStatus.Success, TimeSpan.Zero),
            };

            var props_t2 = new IDataModelValue[]
            {
                Property<Fault>.Create(GenericBurner.ignition_fault.measure, Fault.NotFaulted, TimeSpan.Zero),
                Property<MeasureStatus>.Create(GenericBurner.ignition_fault.status, MeasureStatus.Success, TimeSpan.Zero),
            };

            var slaveController =
                DefineControllerInState(State.ResetPerformed)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ReadMainFirmwareSimulation()
                    .Returning(props_t1)
                    .Returning(props_t1)
                    .ThenReturning(props_t2)
                    .ThenReturning(props_t2)
                    .EndSimulation()
                    .BuildSlaveController();

            var t1 = EventSystemTicked(1000, TimeSpan.Zero);
            {
                var resultingEvents = slaveController.HandleDomainEvent(t1);
                var expectedEvents = new DomainEvent[]
                {
                    SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                };
                Check.That(slaveController.CurrentState).IsEqualTo(State.ResetPerformed);

                Check.That(resultingEvents).Contains(expectedEvents);
                
                Check.That(resultingEvents).Not.Contains(NotFaultedDetected.Create(DeviceNode,GenericBurner));
            }


            var t2 = EventSystemTicked(1000, TimeSpan.Zero);
            {
                var resultingEvents = slaveController.HandleDomainEvent(t2);
                var expectedEvents = new DomainEvent[]
                {
                    SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                    NotFaultedDetected.Create(DeviceNode, GenericBurner),
                    EventPropertyChanged(TimeSpan.Zero, DeviceNode.Urn,
                        (GenericBurner.ignition_fault.measure, Fault.NotFaulted)),
                };
                Check.That(slaveController.CurrentState).IsEqualTo(ResetPerformed);

                Check.That(resultingEvents)
                    .Contains(expectedEvents);
            }
        }

        [Test]
        public void should_transition_to_waiting_ignition_when_not_faulted_detected()
        {
            var slaveController =
                DefineControllerInState(State.CheckReadiness)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .BuildSlaveController();
            var trigger = NotFaultedDetected.Create(DeviceNode, GenericBurner);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (GenericBurner.status, GasBurnerStatus.Ready)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(WaitingIgnition);

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        
        [TestCase(Supplying)]
        [TestCase(Resetting)]
        [TestCase(CheckReadiness)]
        [TestCase(WaitingIgnition)]
        [TestCase(Igniting)]
        [TestCase(Faulted)]
        public void should_switch_off_and_fan_throttle_to_zero(State currentState)
        {
            var slaveController = DefineControllerInState(currentState)
                .ForSimulatedSlave(DeviceNode, GenericBurner)
                .ExecuteCommandSimulation(test_model.burner.fan._throttle).ReturningSuccessResult()
                .ExecuteCommandSimulation(GenericBurner._private<brahma>()._power).ReturningSuccessResult()
                .BuildSlaveController();
            
            var trigger = EventCommandRequested(GenericBurner._supply, PowerSupply.Off, TimeSpan.Zero);
            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, new CommunicationDetails(2,0)),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (test_model.burner.fan._throttle.measure, Percentage.FromFloat(0.0f).Value),
                    (test_model.burner.fan._throttle.status, MeasureStatus.Success),
                    (GenericBurner._private<brahma>()._power.measure, PowerSupply.Off),
                    (GenericBurner._private<brahma>()._power.status, MeasureStatus.Success),
                    (GenericBurner.status, GasBurnerStatus.NotSupplied)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(StandBy);

            Assert.That(resultingEvents, Is.EqualTo(expectedEvents));
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
            
        }
        
        [Test]
        public void should_switch_off_when_ignited()
        {
            var slaveController = DefineControllerInState(Ignited)
                .ForSimulatedSlave(DeviceNode,GenericBurner)
                .ExecuteCommandSimulation(test_model.burner.fan._throttle).ReturningSuccessResult()
                .ExecuteCommandSimulation(GenericBurner._private<brahma>()._power).ReturningSuccessResult()
                .ExecuteCommandSimulation(GenericBurner._private<brahma>()._stop).ReturningSuccessResult()
                .BuildSlaveController();
            
            var trigger = EventCommandRequested(GenericBurner._supply, PowerSupply.Off, TimeSpan.Zero);
            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, new CommunicationDetails(2,0)),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (GenericBurner._private<brahma>()._stop.measure, default(NoArg)),
                    (GenericBurner._private<brahma>()._stop.status, MeasureStatus.Success),
                    (test_model.burner.fan._throttle.measure, Percentage.FromFloat(0.0f).Value),
                    (test_model.burner.fan._throttle.status, MeasureStatus.Success),
                    (GenericBurner._private<brahma>()._power.measure, PowerSupply.Off),
                    (GenericBurner._private<brahma>()._power.status, MeasureStatus.Success),
                    (GenericBurner.status, GasBurnerStatus.NotSupplied)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(StandBy);

            Check.That(resultingEvents)
                .ContainsExactly(expectedEvents);
            
        }
        
        
        
        [Test]
        public void should_transition_to_waiting_ignition_when_receives_stop_command()
        {
            var slaveController =
                DefineControllerInState(Ignited)
                    .ForSimulatedSlave(DeviceNode, GenericBurner)
                    .ExecuteCommandSimulation(GenericBurner._private<brahma>()._stop).ReturningSuccessResult()
                    .BuildSlaveController();

            var trigger = EventCommandRequested(GenericBurner._stop_ignition, default(NoArg), TimeSpan.Zero);

            var resultingEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (GenericBurner._private<brahma>()._stop.measure, default(NoArg)),
                    (GenericBurner._private<brahma>()._stop.status, MeasureStatus.Success),
                    (GenericBurner.status, GasBurnerStatus.Ready)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(WaitingIgnition);

            Check.That(resultingEvents)
                .ContainsExactly(expectedEvents);
        }
    }
    
    
}