using System;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class AlwaysTests
    {
        private always self = examples.always;

        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(always).Assembly);
        }

        [Test]
        public void set_two_targeted_properties()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (self.prop25, Temperature.Create(25f)),
                    (self.propA, Temperature.Create(24f))))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (self.xprop, Literal.Create("Value1")),
                    (self.yprop, Literal.Create("Otherwise")))
            );
        }

        [Test]
        public void set_should_not_be_computed_when_trigger_properties_did_not_changed()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (self.xprop, "toto")))
            ).IsEmpty();
        }


        [Test]
        public void set_when_third_condition_matches()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (self.prop25, Temperature.Create(25f)),
                    (self.prop100, Temperature.Create(100f)),
                    (self.propA, Temperature.Create(50f))))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (self.xprop, Literal.Create("Value3")),
                    (self.yprop, Literal.Create("Otherwise")))
            );
        }

        [Test]
        public void set_otherwise_value_when_no_condition_has_matched()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (self.prop25, Temperature.Create(25f)),
                    (self.prop100, Temperature.Create(100f)),
                    (self.propA, Temperature.Create(110f))))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (self.xprop, Literal.Create("Value4")),
                    (self.yprop, Literal.Create("Otherwise")))
            );
        }

        [Test]
        public void example_of_subsystem_publishing_an_public_state_based_on_private_state()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventCommandRequested(self._activate, default, TimeSpan.Zero))
            ).Contains(
                EventPropertyChanged(TimeSpan.Zero,
                    (self.state, SubsystemState.Create(AlwaysSubsystem.PrivateState.Aa))),
                EventPropertyChanged(TimeSpan.Zero,
                    (self.always_public_state, AlwaysSubsystem.PublicState.PublicA))
            );
        }

        [Test]
        public void should_compute_only_if_with_urn_is_available()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (self.prop25, Temperature.Create(25f)),
                    (self.propC, Temperature.Create(24f)),
                    (self.yprop_default, Literal.Create("Default"))))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (self.xprop, Literal.Create("Value4")),
                    (self.yprop, Literal.Create("Default")))
            );
        }

        [Test]
        public void should_compute_function_when_is_available()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new AlwaysSubsystem());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (self.func, new FunctionDefinition(new (string Name, float Value)[] {("a0", 0.1f), ("a1", 0.5f)})),
                    (self.tprop, Percentage.FromFloat(0.4f).Value)))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (self.zprop, Percentage.FromFloat(0.3f).Value))
            );
        }
    }
}