using System;
using System.Collections.Generic;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Driver;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Scheduling;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.FmuDriver.Tests
{
    [TestFixture]
    [Ignore("fmu")]
    public class ReadStateServiceTests
    {
        private IClock _clock;

        private ModelFactory _modelFactory { get; set; }
        private FmuContext _fmuContext { get; set; }
        private FmuDriverSettings _fmuDriverSettings { get; set; }
        private DriverFmuModuleDefinition DriverFmuModuleDefinition { get; set; }

        [SetUp]
        public void Init()
        {
            _modelFactory = new ModelFactory(Assembly.GetAssembly(typeof(fake)));
            _clock = VirtualClock.Create();
            DriverFmuModuleDefinition = new FakeDriverFmuModuleDefinition();
            _fmuContext = new FmuContext(_clock, DriverFmuModuleDefinition);
            _fmuContext.FmuInstance.CreateNewSimulation(_fmuContext);
            _fmuDriverSettings = new FmuDriverSettings { SimulationTimeStep = 0.1 };
        }

        [TearDown]
        public void End()
        {
            _fmuContext.Dispose();
        }

        [Test]
        public void assert_fmu_model_variables_is_correctly_mapped()
        {
            var sut = FmuService.ReadState(
                DriverFmuModuleDefinition, _modelFactory, _fmuContext.FmuInstance, _fmuContext.FmuReadVariables, _clock, _fmuDriverSettings);
            var trigger = new Idle(_clock.Now(), 0, 0);
            Check.ThatCode(() => sut.Run(trigger)).Not.Throws<KeyNotFoundException>();
        }

        [Test]
        public void sending_status_success()
        {
            var sut = FmuService.ReadState(DriverFmuModuleDefinition, _modelFactory, _fmuContext.FmuInstance, _fmuContext.FmuReadVariables, _clock,
                _fmuDriverSettings);
            var trigger = new Idle(_clock.Now(), 0, 0);
            var expected = new DomainEvent[]
            {
                PropertiesChanged.Create(InternalUnitSimulatedDataSample.DecodedRegistersNominalCase(_clock.Now()), TimeSpan.FromSeconds(0.1))
            };
            var result = sut.Run(trigger);
            Check.That(result).ContainsExactly(expected);
        }

        [Test]
        public void read_state_return_the_right_number_of_events()
        {
            var sut = FmuService.ReadState(DriverFmuModuleDefinition, _modelFactory, _fmuContext.FmuInstance, _fmuContext.FmuReadVariables, _clock,
                _fmuDriverSettings);
            var result = sut.Run(new Idle(_clock.Now(), 0, 0));
            Check.That(result.Length).IsStrictlyGreaterThan(0);
        }

        [Test]
        public void try_40000_seconds_of_fmu_simulation_not_throws_any_exception()
        {
            _fmuContext.FmuInstance.AdvanceTime(100);
            var sut = FmuService.ReadState(DriverFmuModuleDefinition, _modelFactory, _fmuContext.FmuInstance, _fmuContext.FmuReadVariables, _clock,
                _fmuDriverSettings);
            Check.ThatCode(() => sut.Run(new Idle(_clock.Now(), 0, 0))).Not.ThrowsAny();
        }

        [Test]
        public void query_state_requested_are_blocked_on_create()
        {
            var queryStateRequested = new Idle(TimeSpan.Zero, 0, 0);
            var output = FmuService.CanHandleQuery(_fmuContext.FmuInstance)(queryStateRequested);
            Check.That(output).IsFalse();
        }

        [Test]
        public void query_state_should_advance_time()
        {
            new QueryStateRequested(TimeSpan.Zero);
            var sut = FmuService.ReadState(DriverFmuModuleDefinition, _modelFactory, _fmuContext.FmuInstance, _fmuContext.FmuReadVariables, _clock,
                _fmuDriverSettings);
            var startTime = _clock.Now();
            var result = sut.Run(new Idle(startTime, 0, 0));
            Check.That(_clock.Now())
                .IsEqualTo(startTime.Add(TimeSpan.FromSeconds(_fmuDriverSettings.SimulationTimeStep)));
        }
    }
}