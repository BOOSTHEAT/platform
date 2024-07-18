using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.BHBoard.Controller, ImpliciX.RTUModbus.Controllers.BHBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;

namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class FilterOutputDomainEventsTests
    {

        [Test]
        public void should_not_send_measure_statuses_if_not_change_between_consecutive_reads()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ReadMainFirmwareSimulation()
                        .Returning(SuccessRead_1)
                        .ThenReturning(SuccessRead_2)
                        .ThenReturning(FailedRead_3)
                        .ThenReturning(FailedRead_4)
                        .ThenReturning(SuccessRead_5)
                        .EndSimulation()
                .BuildSlaveController();

            var readResults = ReadMany(slaveController, 5);
            var expectedResults = new DomainEvent[][]
            {
                ExpectedEvents(EventPropertyChanged(test_model.software.fake_daughter_board.Urn.Plus("FakeBoard_Name"), SuccessRead_1, TimeSpan.Zero), Time(1)),
                ExpectedEvents(EventPropertyChanged(test_model.software.fake_daughter_board.Urn.Plus("FakeBoard_Name"), SuccessRead_2_WithoutStatusProperties, TimeSpan.Zero), Time(2)),
                ExpectedEvents(EventPropertyChanged(test_model.software.fake_daughter_board.Urn.Plus("FakeBoard_Name"), FailedRead_3, TimeSpan.Zero), Time(3)),
                ExpectedEvents(EventPropertyChanged(test_model.software.fake_daughter_board.Urn.Plus("FakeBoard_Name"), Array.Empty<IDataModelValue>(), TimeSpan.Zero), Time(4)),
                ExpectedEvents(EventPropertyChanged(test_model.software.fake_daughter_board.Urn.Plus("FakeBoard_Name"), SuccessRead_5, TimeSpan.Zero), Time(5)),
            };

            Check.That(readResults).ContainsExactly(expectedResults);
        }

        private static DomainEvent[] ExpectedEvents(PropertiesChanged eventPropertyChanged, TimeSpan at)
        {
            if (eventPropertyChanged.ModelValues.Any())
            {
                return new DomainEvent[]
                {
                    SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, at, Healthy_CommunicationDetails),
                    eventPropertyChanged,
                };
            }

            return new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, at, Healthy_CommunicationDetails),
            };
        }

        private IDataModelValue[] SuccessRead_2_WithoutStatusProperties =>
            new IDataModelValue[]
            {
                Property<Temperature>.Create(test_model.measures.temperature1.measure, Temperature.Create(2f), Time(2)),
                Property<Pressure>.Create(test_model.measures.pressure1.measure, Pressure.FromFloat(3f).Value, Time(2)),
            };


        private  IDataModelValue[] SuccessRead_1 => new IDataModelValue[]
            {
                Property<Temperature>.Create(test_model.measures.temperature1.measure,Temperature.Create(1f),Time(1) ),
                Property<Pressure>.Create(test_model.measures.pressure1.measure,Pressure.FromFloat(1f).Value,Time(1) ),
                Property<MeasureStatus>.Create(test_model.measures.temperature1.status,MeasureStatus.Success,Time(1) ),
                Property<MeasureStatus>.Create(test_model.measures.pressure1.status,MeasureStatus.Success,Time(1)),
            };
            
            private  IDataModelValue[] SuccessRead_2 => new IDataModelValue[]
            {
                Property<Temperature>.Create(test_model.measures.temperature1.measure,Temperature.Create(2f),Time(2) ),
                Property<Pressure>.Create(test_model.measures.pressure1.measure,Pressure.FromFloat(3f).Value,Time(2) ),
                Property<MeasureStatus>.Create(test_model.measures.pressure1.status,MeasureStatus.Success,Time(2)),
                Property<MeasureStatus>.Create(test_model.measures.temperature1.status,MeasureStatus.Success,Time(2) ),
            };
            
            private IDataModelValue[] FailedRead_3 => new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(test_model.measures.temperature1.status,MeasureStatus.Failure,Time(3) ),
                Property<MeasureStatus>.Create(test_model.measures.pressure1.status,MeasureStatus.Failure,Time(3) )
            };
            
            private IDataModelValue[] FailedRead_4 => new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(test_model.measures.temperature1.status,MeasureStatus.Failure,Time(4)),
                Property<MeasureStatus>.Create(test_model.measures.pressure1.status,MeasureStatus.Failure,Time(4) )
            };
            
            private IDataModelValue[] SuccessRead_5 => new IDataModelValue[]
            {
                Property<Temperature>.Create(test_model.measures.temperature1.measure,Temperature.Create(4f),Time(5) ),
                Property<Pressure>.Create(test_model.measures.pressure1.measure,Pressure.FromFloat(5f).Value,Time(5) ),
                Property<MeasureStatus>.Create(test_model.measures.temperature1.status,MeasureStatus.Success,Time(5) ),
                Property<MeasureStatus>.Create(test_model.measures.pressure1.status,MeasureStatus.Success,Time(5))
            };

        
        private List<DomainEvent[]> ReadMany(ISlaveController slaveController, int times)
        {
            var results = new List<DomainEvent[]>();
            var trigger = SystemTicked.Create(1000, 1);
            for (int i = 0; i < times; i++) 
                results.Add(slaveController.HandleDomainEvent(trigger));
            return results;
        }
        
        private TimeSpan Time(int t) => TimeSpan.FromSeconds(t);
    }
}