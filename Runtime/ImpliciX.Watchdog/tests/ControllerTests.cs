using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Watchdog.Tests
{
    public class ControllerTests
    {
        [TestCaseSource(nameof(PropertyChangeCanHandleCases))]
        public void should_handle_alarm_activation(IEnumerable<IDataModelValue> properties, bool expectedHandling)
        {
            Check
                .That(_controller.CanHandleProperties(PropertiesChanged.Create(properties, TimeSpan.Zero)))
                .IsEqualTo(expectedHandling);
        }

        static IEnumerable<object> PropertyChangeCanHandleCases()
        {
            var someString = Property<string>.Create(PropertyUrn<string>.Build("whatever"), string.Empty, TimeSpan.Zero);
            yield return new object[] {new IDataModelValue[] { }, false};
            yield return new object[] {new IDataModelValue[] {Alarms[1].Active}, true};
            yield return new object[] {new IDataModelValue[] {Alarms[1].Inactive}, true};
            yield return new object[] {new IDataModelValue[] {someString}, false};
            yield return new object[] {new IDataModelValue[] {someString, Alarms[1].Active}, true};
            yield return new object[] {new IDataModelValue[] {someString, Alarms[1].Inactive}, true};
            yield return new object[] {new IDataModelValue[] {Alarms[0].Inactive}, false};
        }

        [TestCaseSource(nameof(PropertyChangeHandleCases))]
        public void should_publish_IO_panic_for_monitored_active_alarms(IDataModelValue[] incomingProperties, string[] panicModules)
        {
            Check
                .That(_controller.HandlePropertiesChanged(PropertiesChanged.Create(incomingProperties, TimeSpan.Zero)))
                .ContainsExactly(panicModules.Select(m => ModulePanic.Create(m, TimeSpan.Zero))
                );
        }

        static IEnumerable<object> PropertyChangeHandleCases()
        {
            yield return new object[] {new[] {Alarms[1].Active}, new[] {"ModuleA"}};
            yield return new object[] {new[] {Alarms[3].Active}, new[] {"ModuleA"}};
            yield return new object[] {new[] {Alarms[2].Active}, new[] {"ModuleB"}};
            yield return new object[] {new[] {Alarms[4].Active}, new[] {"ModuleB"}};
            yield return new object[] {new[] {Alarms[1].Inactive}, new string[] { }};
            yield return new object[] {new[] {Alarms[0].Active, Alarms[1].Active, Alarms[2].Inactive}, new[] {"ModuleA"}};
            yield return new object[] {new[] {Alarms[0].Inactive, Alarms[1].Active, Alarms[2].Inactive}, new[] {"ModuleA"}};
            yield return new object[] {new[] {Alarms[0].Inactive, Alarms[1].Active, Alarms[2].Active}, new[] {"ModuleA", "ModuleB"}};
            yield return new object[] {new[] {Alarms[2].Active, Alarms[1].Active}, new[] {"ModuleB", "ModuleA"}};
            yield return new object[] {new[] {Alarms[4].Active, Alarms[3].Active, Alarms[2].Active}, new[] {"ModuleB", "ModuleA"}};
        }

        [Test]
        public void should_restart_app_when_panic_reached_timeout()
        {
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Active}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(1000));
            Check
                .That(_restartCalledNumber)
                .IsEqualTo(1);
        }

        [Test]
        public void should_not_restart_app_when_panic_not_reached_timeout()
        {
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Active}, TimeSpan.Zero));
            Check
                .That(_restartCalledNumber)
                .IsEqualTo(0);
        }

        [Test]
        public void should_not_restart_app_when_alarm_is_down_before_timeout()
        {
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Active}, TimeSpan.Zero));
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Inactive}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(1000));
            Check
                .That(_restartCalledNumber)
                .IsEqualTo(0);
        }

        [Test]
        public void should_restart_app_only_for_concerned_timeout()
        {
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Active}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(500));
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[2].Active}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(250));
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Inactive}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(750));
            Check
                .That(_restartCalledNumber)
                .IsEqualTo(1);
        }

        [Test]
        public void timer_are_by_alarms_and_not_by_module()
        {
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Active}, TimeSpan.Zero));
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[3].Active}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(1000));
            Check
                .That(_restartCalledNumber)
                .IsEqualTo(2);
        }
        
        [Test]
        public void two_alarms_for_the_same_module_work_independently()
        {
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Active}, TimeSpan.Zero));
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[3].Active}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(500));
            _controller.HandlePropertiesChanged(PropertiesChanged.Create(new[] {Alarms[1].Inactive}, TimeSpan.Zero));
            _clock.Advance(TimeSpan.FromMilliseconds(500));
            Check
                .That(_restartCalledNumber)
                .IsEqualTo(1);
        }

        [SetUp]
        public void Setup()
        {
            _restartCalledNumber = 0;
            _clock = VirtualClock.Create();
            _controller = new Controller(
                new Settings
                {
                    Modules = new Dictionary<string, string>
                    {
                        {"A", "ModuleA"},
                        {"B", "ModuleB"},
                        {"B0", "ModuleB"}
                    },
                    PanicDelayBeforeRestart = 1000
                },
                new WatchdogModuleDefinition
                {
                    InputOutputPanic = new Dictionary<Urn, string>
                    {
                        {Alarms[1].Node.state, "A"},
                        {Alarms[2].Node.state, "B"},
                        {Alarms[3].Node.state, "A"},
                        {Alarms[4].Node.state, "B0"}
                    }
                },
                _clock,
                (_) => _restartCalledNumber += 1
            );
        }


        private Controller _controller;
        private VirtualClock _clock;
        private int _restartCalledNumber;
        static readonly AlarmDeclaration[] Alarms;

        class AlarmDeclaration
        {
            public AlarmDeclaration(int i, ModelNode parent)
            {
                Node = new AlarmNode($"C00{i}", parent);
                Active = Property<AlarmState>.Create(Node.state, AlarmState.Active, TimeSpan.Zero);
                Inactive = Property<AlarmState>.Create(Node.state, AlarmState.Inactive, TimeSpan.Zero);
            }

            public AlarmNode Node { get; }
            public IDataModelValue Active { get; }
            public IDataModelValue Inactive { get; }
        }


        static ControllerTests()
        {
            var root = new RootModelNode("root");
            Alarms = Enumerable.Range(0, 5).Select(i => new AlarmDeclaration(i, root)).ToArray();
        }
    }
}