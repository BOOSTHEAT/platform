using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Doubles;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.VendorBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.VendorBoard.Controller, ImpliciX.RTUModbus.Controllers.VendorBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.VendorBoard
{
    [TestFixture]
    public class FilterOutputDomainEventsTests
    {
        [Test]
        public void should_not_send_measure_statuses_if_not_change_between_consecutive_reads()
        {
            var slaveController =
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_other_board)
                        .WithReadPeaceSettings(1)
                        .ReadMainFirmwareSimulation()
                            .Returning(SuccessRead_1)
                            .ThenReturning(SuccessRead_2)
                            .ThenReturning(FailedRead_3)
                            .ThenReturning(FailedRead_4)
                            .ThenReturning(SuccessRead_5)
                            .EndSimulation()
                    .BuildSlaveController();

            var readResults = slaveController.ReadMany(5);
            var expectedResults = new DomainEvent[][]
            {
                ExpectedEvents(EventPropertyChanged(slaveController.Group, SuccessRead_1, Time(1)), Time(1)),
                ExpectedEvents(EventPropertyChanged(slaveController.Group, SuccessRead_2_WithoutStatusProperties, Time(2)), Time(2)),
                ExpectedEvents(EventPropertyChanged(slaveController.Group, FailedRead_3, Time(3)), Time(3)),
                ExpectedEvents(SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_other_board, Time(4), Healthy_CommunicationDetails), Time(4)),
                ExpectedEvents(EventPropertyChanged(slaveController.Group, SuccessRead_5, Time(5)), Time(5)),
            };
            
            Assert.That(readResults, Is.EqualTo(expectedResults));

            Check.That(readResults).ContainsExactly(expectedResults);
        }

        private static DomainEvent[] ExpectedEvents(SlaveCommunicationOccured slaveCommunicationOccured, TimeSpan at)
        {
            return new DomainEvent[]
            {
                slaveCommunicationOccured
            };
        }

        private static DomainEvent[] ExpectedEvents(PropertiesChanged eventPropertyChanged, TimeSpan at)
        {
            return new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_other_board, at, Healthy_CommunicationDetails),
                eventPropertyChanged,
            };
        }

        private IDataModelValue[] SuccessRead_2_WithoutStatusProperties =>
            new IDataModelValue[]
            {
                Property<Temperature>.Create(test_model.measures.temperature1.measure, Temperature.Create(2f), Time(2)),
                Property<Pressure>.Create(test_model.measures.pressure1.measure, Pressure.FromFloat(3f).Value, Time(2)),
            };


        private IDataModelValue[] SuccessRead_1 => new IDataModelValue[]
        {
            Property<Temperature>.Create(test_model.measures.temperature1.measure, Temperature.Create(1f), Time(1)),
            Property<Pressure>.Create(test_model.measures.pressure1.measure, Pressure.FromFloat(1f).Value, Time(1)),
            Property<MeasureStatus>.Create(test_model.measures.temperature1.status, MeasureStatus.Success, Time(1)),
            Property<MeasureStatus>.Create(test_model.measures.pressure1.status, MeasureStatus.Success, Time(1)),
        };

        private IDataModelValue[] SuccessRead_2 => new IDataModelValue[]
        {
            Property<Temperature>.Create(test_model.measures.temperature1.measure, Temperature.Create(2f), Time(2)),
            Property<Pressure>.Create(test_model.measures.pressure1.measure, Pressure.FromFloat(3f).Value, Time(2)),
            Property<MeasureStatus>.Create(test_model.measures.pressure1.status, MeasureStatus.Success, Time(2)),
            Property<MeasureStatus>.Create(test_model.measures.temperature1.status, MeasureStatus.Success, Time(2)),
        };

        private IDataModelValue[] FailedRead_3 => new IDataModelValue[]
        {
            Property<MeasureStatus>.Create(test_model.measures.temperature1.status, MeasureStatus.Failure, Time(3)),
            Property<MeasureStatus>.Create(test_model.measures.pressure1.status, MeasureStatus.Failure, Time(3))
        };

        private IDataModelValue[] FailedRead_4 => new IDataModelValue[]
        {
            Property<MeasureStatus>.Create(test_model.measures.temperature1.status, MeasureStatus.Failure, Time(4)),
            Property<MeasureStatus>.Create(test_model.measures.pressure1.status, MeasureStatus.Failure, Time(4))
        };

        private IDataModelValue[] SuccessRead_5 => new IDataModelValue[]
        {
            Property<Temperature>.Create(test_model.measures.temperature1.measure, Temperature.Create(4f), Time(5)),
            Property<Pressure>.Create(test_model.measures.pressure1.measure, Pressure.FromFloat(5f).Value, Time(5)),
            Property<MeasureStatus>.Create(test_model.measures.temperature1.status, MeasureStatus.Success, Time(5)),
            Property<MeasureStatus>.Create(test_model.measures.pressure1.status, MeasureStatus.Success, Time(5))
        };


 

        private TimeSpan Time(int t) => TimeSpan.FromSeconds(t);
    }
}