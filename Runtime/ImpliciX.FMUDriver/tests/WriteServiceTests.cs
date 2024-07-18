using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.FmuDriver.Tests
{
    [TestFixture]
    [Ignore("fmu")]
    public class WriteServiceTests
    {
        private IClock _clock;
        private ModelFactory _modelFactory { get; set; }
        private FmuContext _fmuContext { get; set; }
        private FmuDriverSettings _fmuDriverSettings { get; set; }
        private SpyFmuIO FmuIo { get; set; }
        private DriverFmuModuleDefinition DriverFmuModuleDefinition { get; set; }


        [SetUp]
        public void Init()
        {
            _modelFactory = new ModelFactory(Assembly.GetAssembly(typeof(fake)));
            _clock = VirtualClock.Create();
            DriverFmuModuleDefinition = new FakeDriverFmuModuleDefinition();
            _fmuContext = new FmuContext(_clock,DriverFmuModuleDefinition);
            _fmuContext.FmuInstance.CreateNewSimulation(_fmuContext);
            _fmuDriverSettings = new FmuDriverSettings {SimulationTimeStep = 0.1};
            FmuIo = new SpyFmuIO();
        }

        [TearDown]
        public void End()
        {
            _fmuContext.Dispose();
        }

        [TestCase("fake:POWER","On", "true")]
        [TestCase("fake:THROTTLE",".5", "true")]
        [TestCase("fake:SOMETHING",".", "false")]
        public void can_execute_command(string urn, string value, bool expected)
        {
            var commandRequested = EventCommandRequested(urn, value, _clock.Now());
            var output = FmuService.CanExecuteCommand(DriverFmuModuleDefinition)(commandRequested);
            Check.That(output).IsEqualTo(expected);
        }

        [Test]
        public void bool_is_written()
        {
            var fmuVariables = new[] {_fmuContext.FmuWrittenVariables.First(v => v.Name == "OnOff_Pompe_ECS")};
            var trigger = EventCommandRequested(fake._power, PowerSupply.On, _clock.Now());
            var sut = FmuService.SendFmuDriverCommand(
                _modelFactory, _fmuContext, FmuIo, fmuVariables, _clock);
            sut.Run(trigger);
            Check.That(FmuIo.WrittenBools).ContainsExactly(true);
        }

        [Test]
        public void float_is_written()
        {
            var fmuVariables = new[] {_fmuContext.FmuWrittenVariables.First(v => v.Name == "Signal_Aux")};
            var trigger = EventCommandRequested(fake._throttle, Percentage.FromFloat(.5f).Value, _clock.Now());
            var sut = FmuService.SendFmuDriverCommand(_modelFactory, _fmuContext, FmuIo, fmuVariables, _clock);
            sut.Run(trigger);
            Check.That(FmuIo.WrittenReals).ContainsExactly(.5f);
        }

        [Test]
        public void success_is_sent()
        {
            var fmuVariables = new[] {_fmuContext.FmuWrittenVariables.First(v => v.Name == "Signal_Aux")};
            var trigger = EventCommandRequested(fake._throttle, Percentage.FromFloat(.5f).Value, _clock.Now());
            var sut = FmuService.SendFmuDriverCommand(_modelFactory, _fmuContext, FmuIo, fmuVariables, _clock);
            var expected = EventPropertyChanged(fake._throttle.measure, Percentage.FromFloat(.5f).Value, _clock.Now());
            var events = sut.Run(trigger);
            Check.That(events).ContainsExactly(expected);
        }
    }
}