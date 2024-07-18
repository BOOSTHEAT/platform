using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Fixtures.Helpers;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.BigPicture
{
    [TestFixture]
    public class ControlSystemTests
    {
        private readonly TimeSpan _testTime = TimeSpan.Zero;
        private List<IImpliciXSystem> _fakeSubSystems;

        [Test]
        public void every_subsystems_should_handle_commandRequest()
        {
            var sut = new ControlSystem() { SubSystems = _fakeSubSystems };
            var trigger = default(CommandRequested);
            var events = sut.PlayEvents(trigger);
            var checkEvents = _fakeSubSystems.SelectMany(subsystem => subsystem.PlayEvents(trigger)).ToArray();
            Check.That(checkEvents).IsEqualTo(events);
        }

        [Test]
        public void every_subsystems_should_handle_propertyChanged()
        {
            var sut = new ControlSystem() { SubSystems = _fakeSubSystems };
            var eventChanged = PropertiesChangedHelper.CreatePropertyChanged(_testTime, "testUrn", "testValue");
            var events = sut.PlayEvents(eventChanged);
            var checkEvents = _fakeSubSystems.SelectMany(subsystem => subsystem.PlayEvents(eventChanged)).ToArray();
            Check.That(events).IsEqualTo(checkEvents);
        }

        [Test]
        public void control_system_aggregate_propertyChanged()
        {
            var sut = new ControlSystem() { SubSystems = new List<IImpliciXSystem> { new FakeSubsystemMultipleEventSender() } };
            var resultingEvents = sut.PlayEvents(EventPropertyChanged(examples.dummy, 0.5f, _testTime));
            Check.That(resultingEvents).CountIs(1);
            var expectedEvent = PropertiesChanged.Join(new[]
            {
                EventPropertyChanged(examples.dummy, 0.1f, TimeSpan.Zero),
                EventPropertyChanged(examples.always.prop25, 25f, TimeSpan.Zero),
                EventPropertyChanged(examples.always.prop100, 110f, TimeSpan.Zero)
            }).GetValue();
            Check.That(resultingEvents[0]).IsEqualTo(expectedEvent);
        }

        [Test]
        public void every_subsystems_should_handle_timeOutOccured()
        {
            var sut = new ControlSystem() { SubSystems = _fakeSubSystems };
            var trigger = TimeoutOccured.Create(Urn.BuildUrn("testUrn"), _testTime, Guid.Empty);
            var values = sut.PlayEvents(trigger);
            var checkEvents = _fakeSubSystems.SelectMany(subsystem => subsystem.PlayEvents(trigger)).ToArray();
            Check.That(values).IsEqualTo(checkEvents);
        }

        [Test]
        public void activation_test()
        {
            var sut = new ControlSystem() { SubSystems = _fakeSubSystems };
            var results = sut.Activate();
            Check.That(results.Length).IsEqualTo(_fakeSubSystems.Count);
        }

        [Test]
        public void constant_parameters_urns_are_managed()
        {
            var parameters = new parameters(new constant());
            var propertiesUrn = new List<string>();
            get_all_properties_urns(parameters, propertiesUrn);
            propertiesUrn.Remove(constant.parameters.none);
            var properties = new ExecutionEnvironment();
            propertiesUrn.ForEach(p => Check.That(properties.GetProperty(p).IsSuccess).IsTrue());
        }

        private static void get_all_properties_urns(object obj, ICollection<string> propertiesUrn)
        {
            var excludedTypes = new[] { "Urn", "ModelNode", "String" };
            var propInfos = obj.GetType().GetProperties();
            foreach (var propertyInfo in propInfos)
            {
                if (propertyInfo.PropertyType.Name.Split('`')[0] == "PropertyUrn")
                    propertiesUrn.Add(((Urn) propertyInfo.GetValue(obj))?.Value);
                else if (!excludedTypes.Contains(propertyInfo.PropertyType.Name))
                    get_all_properties_urns(propertyInfo.GetValue(obj, null), propertiesUrn);
            }
        }

        [TestCase("constant:parameters:percentage:zero", 8f)]
        [TestCase("constant:parameters:percentage:one", 0.01f)]
        public static void get_const_percentage(string urn, float percentage)
        {
            var sut = new ExecutionEnvironment();
            var result = sut.GetProperty(urn);
            var expected = Result<IDataModelValue>.Create(Property<Percentage>.Create(PropertyUrn<Percentage>.Build(urn), Percentage.FromFloat(percentage).Value,
                TimeSpan.Zero));
        }

        [TestCase("constant:parameters:displacement_queue:zero", 0)]
        [TestCase("constant:parameters:displacement_queue:one", 1)]
        public void get_const_displacement_queue(string urn, short expectedValue)
        {
            var sut = new ExecutionEnvironment();
            var result = sut.GetProperty(urn);
            var expected = Result<IDataModelValue>.Create(Property<DisplacementQueue>.Create(PropertyUrn<DisplacementQueue>.Build(urn),
                DisplacementQueue.FromShort(expectedValue).GetValueOrDefault(), TimeSpan.Zero));
            Check.That(result.Value.ModelValue()).IsEqualTo(expected.Value.ModelValue());
        }

        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(examples).Assembly);
            _fakeSubSystems = new List<IImpliciXSystem> { new FakeSubSystem("SubSystem1"), new FakeSubSystem("SubSystem2") };
        }

        private class FakeSubsystemMultipleEventSender : IImpliciXSystem
        {
            public DomainEvent[] HandleDomainEvent(DomainEvent @event)
            {
                var propertiesChanged = @event as PropertiesChanged;
                if (propertiesChanged.ModelValues.Count() != 1)
                    return new DomainEvent[] { };
                return new DomainEvent[]
                {
                    EventPropertyChanged(examples.dummy, 0.1f, TimeSpan.Zero),
                    EventPropertyChanged(examples.always.prop25, 25f, TimeSpan.Zero),
                    EventPropertyChanged(examples.always.prop100, 110f, TimeSpan.Zero)
                };
            }

            public bool CanHandle(CommandRequested commandRequested) => true;

            public DomainEvent[] Activate()
            {
                return new DomainEvent[]
                {
                    EventPropertyChanged(examples.simplified_subsystem.state, "A", TimeSpan.Zero)
                };
            }
        }

        private class FakeSubSystem : IImpliciXSystem
        {
            private string _name;

            public FakeSubSystem(string name)
            {
                _name = name;
            }

            private DomainEvent[] HandleCommandRequest(CommandRequested requestEvent)
            {
                return new DomainEvent[] { new FakeDomainEvent(System.Guid.Empty, TimeSpan.Zero) };
            }

            private DomainEvent[] HandleTimeoutOccured(TimeoutOccured timeoutOccured)
            {
                return new DomainEvent[] { new FakeDomainEvent(System.Guid.Empty, TimeSpan.Zero) };
            }

            private DomainEvent[] HandlePropertiesChanged(PropertiesChanged propertiesChanged)
            {
                return new DomainEvent[] { new FakeDomainEvent(System.Guid.Empty, TimeSpan.Zero) };
            }

            private DomainEvent[] HandleSystemTicked(SystemTicked systemTicked)
            {
                throw new NotImplementedException();
            }

            public DomainEvent[] HandleDomainEvent(DomainEvent @event) =>
                @event switch
                {
                    CommandRequested commandRequested => HandleCommandRequest(commandRequested),
                    PropertiesChanged propertiesChanged => HandlePropertiesChanged(propertiesChanged),
                    SystemTicked systemTicked => HandleSystemTicked(systemTicked),
                    TimeoutOccured timeoutOccured => HandleTimeoutOccured(timeoutOccured),
                    _ => Array.Empty<DomainEvent>()
                };

            public bool CanHandle(CommandRequested commandRequested) => true;

            public DomainEvent[] Activate()
            {
                return new DomainEvent[]
                {
                    EventPropertyChanged(examples.simplified_subsystem.state, SubsystemState.Create(StateTest.A), TimeSpan.Zero)
                };
            }
        }

        private class FakeDomainEvent : PublicDomainEvent
        {
            public FakeDomainEvent(Guid eventId, TimeSpan at) : base(eventId, at)
            {
            }

            private bool Equals(FakeDomainEvent other)
            {
                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((FakeDomainEvent) obj);
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }
    }
}