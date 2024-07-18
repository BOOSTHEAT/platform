using System;
using ImpliciX.Language.Driver;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.FmuDriver.Tests
{
    [TestFixture]
    [Ignore("fmu")]
    public class StartStopFmuTests
    {
        [SetUp]
        public void Init()
        {
            DriverFmuModuleDefinition = new FakeDriverFmuModuleDefinition();
            _clock = VirtualClock.Create();
            _fmuContext = new FmuContext(_clock, DriverFmuModuleDefinition);
        }
        [TearDown]
        public void End()
        {
            _fmuContext.Dispose();
        }
        private DriverFmuModuleDefinition DriverFmuModuleDefinition { get; set; }
        private IClock _clock;
        private FmuContext _fmuContext { get; set; }

        [Test]
        public void is_FMU_command_configuration()
        {
            var start_event = EventCommandRequested(fake.simulation_start, default, TimeSpan.Zero);
            var stop_event = EventCommandRequested(fake.simulation_stop, default, TimeSpan.Zero);

            var random_event = EventCommandRequested(fake._something, default, TimeSpan.Zero);
            Check.That(FmuService.CanExecuteCommand(DriverFmuModuleDefinition)(start_event)).IsTrue();
            Check.That(FmuService.CanExecuteCommand(DriverFmuModuleDefinition)(stop_event)).IsTrue();
            Check.That(FmuService.CanExecuteCommand(DriverFmuModuleDefinition)(random_event)).IsFalse();
        }

        [Test]
        public void on_start_should_start_simulation()
        {
            var fmuSimulation = new SpyFmuSimulation();
            var sut = FmuService.SendFmuCommand(DriverFmuModuleDefinition, _fmuContext, fmuSimulation);
            var evt = EventCommandRequested(fake.simulation_start, default, TimeSpan.Zero);
            sut.Run(evt);

            Check.That(fmuSimulation.FmuStarted).IsTrue();
            Check.That(fmuSimulation.countSimulation).IsEqualTo(0);
        }

        [Test]
        public void on_stop_should_stop_simulation()
        {
            var fmuSimulation = new SpyFmuSimulation();
            var sut = FmuService.SendFmuCommand(DriverFmuModuleDefinition, _fmuContext, fmuSimulation);
            var start_evt = EventCommandRequested(fake.simulation_start, default, TimeSpan.Zero);
            var stop_evt = EventCommandRequested(fake.simulation_stop, default, TimeSpan.Zero);
            sut.Run(start_evt);
            sut.Run(stop_evt);

            Check.That(fmuSimulation.FmuStarted).IsFalse();
            Check.That(fmuSimulation.countSimulation).IsEqualTo(1);
        }
    }
}