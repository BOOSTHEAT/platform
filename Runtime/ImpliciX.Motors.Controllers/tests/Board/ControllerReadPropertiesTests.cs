using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Model.MeasureStatus;
using static ImpliciX.Motors.Controllers.Board.State;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.Motors.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.Motors.Controllers.Board.SlaveController, ImpliciX.Motors.Controllers.Board.State>;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    [TestFixture]
    public class ControllerReadPropertiesTests
    {
        [Test]
        public void should_handle_system_ticks_and_trigger_read_measures_at_specified_pace_on_started()
        {
            var slaveController =
                DefineControllerInState(Started)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .WithReadPeaceSettings(2)
                    .ReadRegulationSimulation().Returning(ReadMainFirmwareProperties(),4).EndSimulation()
                    .BuildSlaveController();

            var expected = EventPropertyChanged(ReadMainFirmwareProperties(), TimeSpan.Zero);

            { 
                var trigger = SystemTicked.Create(1000, 1);
                var resultedEvents = slaveController.HandleDomainEvent(trigger);
                Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(0);
            }

            {
                var trigger = SystemTicked.Create(1000, 2);
                var resultedEvents = slaveController.HandleDomainEvent(trigger);
                Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(1);
            }
             
            { 
                var trigger = SystemTicked.Create(1000, 3);
                var resultedEvents = slaveController.HandleDomainEvent(trigger);
                Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(0);
            }
             
            {
                var trigger = SystemTicked.Create(1000, 4);
                var resultedEvents = slaveController.HandleDomainEvent(trigger);
                Check.That(resultedEvents.FilterEvents<PropertiesChanged>()).CountIs(1);
            }
        }

        
        [TestCase(new[] {Success, Success, Success}, true)]
        [TestCase(new[] {Failure, Success, Success}, false)]
        [TestCase(new[] {Success, Failure, Success}, false)]
        [TestCase(new[] {Success, Success, Failure}, false)]
        [TestCase(new[] {Failure, Failure, Success}, false)]
        [TestCase(new[] {Failure, Failure, Failure}, false)]
        public void should_send_motors_status_running_when_measures_are_in_success(MeasureStatus[] measureStatuses, bool expected)
        {
            var slaveController =
                DefineControllerInState(Started)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .WithReadPeaceSettings(1)
                    .ReadRegulationSimulation().Returning(ReadMainFirmwareProperties(measureStatuses)).EndSimulation()
                    .BuildSlaveController();
            
            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var actual = resultedEvents.FilterEvents<PropertiesChanged>().CollectProperties()
                .Contains(Property<MotorsStatus>.Create(test_model.motors.status, MotorsStatus.Running, TimeSpan.Zero));
            Check.That(actual).IsEqualTo(expected);
        }
        
        [Test]
        public void should_send_communication_errors_on_read_failure()
        {
            var slaveController = DefineControllerInState(Started)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .WithReadPeaceSettings(1)
                .ReadRegulationSimulation().WithSlaveCommunicationError().EndSimulation()
                .BuildSlaveController();

            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            Check.That(resultedEvents)
                .Contains(SlaveCommunicationOccured.CreateError(test_model.software.fake_motor_board, TimeSpan.Zero, new CommunicationDetails(0,1)));
        }

        [Test]
        public void should_send_communication_healthy_on_read_success()
        {
            var slaveController = DefineControllerInState(Started)
                .ForSimulatedSlave(test_model.software.fake_motor_board)
                .WithReadPeaceSettings(1)
                .ReadRegulationSimulation().Returning(ReadMainFirmwareProperties()).EndSimulation()
                .BuildSlaveController();

            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);

            Check.That(resultedEvents)
                .Contains(SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, TimeSpan.Zero, new CommunicationDetails(1,0)));
        }

        [Test]
        public void should_not_send_measure_statuses_if_not_change_between_consecutive_reads()
        {
            var slaveController =
                DefineControllerInState(Started)
                    .ForSimulatedSlave(test_model.software.fake_motor_board)
                    .WithReadPeaceSettings(1)
                    .ReadRegulationSimulation()
                    .Returning(Motor1Properties(Success))
                    .ThenReturning(Motor1Properties(Success))
                    .ThenReturning(Motor1Properties(Failure))
                    .ThenReturning(Motor1Properties(Failure))
                    .ThenReturning(Motor1Properties(Success))
                    .EndSimulation()
                    .BuildSlaveController();

            var readResults = slaveController.ReadMany(5);
            var expectedResults = new DomainEvent[][]
            {
                ExpectedEvents(EventPropertyChanged(slaveController.Group, Motor1AndRunningStatusProperties(), Time(1)), Time(1)),
                ExpectedEvents(EventPropertyChanged(slaveController.Group, Motor1AndRunningStatusProperties(withStatus:false), Time(2)), Time(2)),
                ExpectedEvents(EventPropertyChanged(slaveController.Group, Motor1Properties(Failure), Time(3)), Time(3)),
                ExpectedEvents(SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, Time(4), new CommunicationDetails(1,0)), Time(4)),
                ExpectedEvents(EventPropertyChanged(slaveController.Group, Motor1AndRunningStatusProperties(), Time(5)), Time(5)),
            };

            Assert.That(readResults[0], Is.EqualTo(expectedResults[0]));
            Check.That(readResults).ContainsExactly(expectedResults);
        }

        private IDataModelValue[] Motor1AndRunningStatusProperties(bool withStatus=true)
        {
            return Motor1Properties(Success, withStatus:withStatus).Append(RunnigStatusProperty()).ToArray();
        }

        private static DomainEvent[] ExpectedEvents(PropertiesChanged eventPropertyChanged, TimeSpan at)
        {

            return new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_motor_board, at, new CommunicationDetails(1,0)),
                eventPropertyChanged
            };
        }
        
        private static DomainEvent[] ExpectedEvents(SlaveCommunicationOccured slaveCommunicationOccured, TimeSpan at)
        {
            return new DomainEvent[]
            {
                slaveCommunicationOccured
            };
        }

        private IDataModelValue RunnigStatusProperty() => Property<MotorsStatus>.Create(test_model.motors.status, MotorsStatus.Running, TimeSpan.Zero);
        
        private IDataModelValue[] ReadMainFirmwareProperties() =>
            Motor1Properties(Success)
                .Concat(Motor2Properties(Success))
                .Concat(Motor3Properties(Success))
                .ToArray();
        
        
        private IDataModelValue[] ReadMainFirmwareProperties(MeasureStatus[] statuses) =>
            Motor1Properties(statuses[0])
                .Concat(Motor2Properties(statuses[1]))
                .Concat(Motor3Properties(statuses[2]))
                .ToArray();

        private IDataModelValue[] Motor1Properties(MeasureStatus status, float speed = 200f, bool withStatus = true)
        {
           return MotorProperties(test_model.motors._1.mean_speed, speed, status, withStatus);
        }
        
        private IDataModelValue[] Motor2Properties(MeasureStatus status, float speed = 200f,bool withStatus = true)
        {
            return MotorProperties(test_model.motors._2.mean_speed, speed, status, withStatus);
        }

        private IDataModelValue[] Motor3Properties(MeasureStatus status, float speed = 200f, bool withStatus = true) => 
            MotorProperties(test_model.motors._3.mean_speed, speed, status, withStatus);

        private static IDataModelValue[] MotorProperties(MeasureNode<RotationalSpeed> measureNode, float speed, MeasureStatus status, bool withStatus)
        {
            return (status, withStatus) switch
            {
                (Success, true) => new IDataModelValue[]
                {
                    Property<RotationalSpeed>.Create(measureNode.measure, RotationalSpeed.FromFloat(speed).Value, TimeSpan.Zero),
                    Property<MeasureStatus>.Create(measureNode.status, MeasureStatus.Success, TimeSpan.Zero),
                },
                (Success, false) => new IDataModelValue[]
                {
                    Property<RotationalSpeed>.Create(measureNode.measure, RotationalSpeed.FromFloat(speed).Value, TimeSpan.Zero),
                },

                (Failure, true)=> new IDataModelValue[]
                {
                    Property<MeasureStatus>.Create(measureNode.status, MeasureStatus.Failure, TimeSpan.Zero),
                },
                (Failure, false)=> new IDataModelValue[0],
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
            };
        }
        
        private TimeSpan Time(int t) => TimeSpan.FromSeconds(t);
    }
}