using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class OnExitTests : SubSystemTests
    {
        [Test]
        public void set_command_with_value()
        {
            var sut = CreateSut(ComplexSubsystem.State.C, new ComplexSubsystem());
            var events = sut.PlayEvents(EventCommandRequested(examples.complex_subsystem._td, default, TestTime));
            var expected = EventPropertyChanged(examples.complex_subsystem.prop1, PowerSupply.Off, TestTime);
            Check.That(events).Contains(expected);
        }
        
        [Test]
        public void set_command_with_no_arg()
        {
            var sut = CreateSut(ComplexSubsystem.State.C, new ComplexSubsystem());
            var events = sut.PlayEvents(EventCommandRequested(examples.complex_subsystem._td, default, TestTime));
            var expected = EventCommandRequested(examples.complex_subsystem._tg, default, TestTime);
            Check.That(events).Contains(expected);
        }
        
        [Test]
        public void set_command_with_property()
        {
          var sut = CreateSut(ComplexSubsystem.State.C, new ComplexSubsystem());
          WithProperties((examples.complex_subsystem.prop2,PowerSupply.Off));
          var events = sut.PlayEvents(EventCommandRequested(examples.complex_subsystem._td, default, TestTime));
          var expected = EventCommandRequested(examples.complex_subsystem._te, PowerSupply.Off, TestTime);
          Check.That(events).Contains(expected);
        }
        
        [Test]
        public void set_property_with_property()
        {
          var sut = CreateSut(ComplexSubsystem.State.C, new ComplexSubsystem());
          WithProperties(
            (examples.complex_subsystem.prop2,PowerSupply.Off),
            (examples.complex_subsystem.prop3,PowerSupply.On)
            );
          var events = sut.PlayEvents(EventCommandRequested(examples.complex_subsystem._td, default, TestTime));
          var expected = EventPropertyChanged(examples.complex_subsystem.prop3, PowerSupply.Off, TestTime);
          Check.That(events).Contains(expected);
        }

        [Test]
        public void on_exit_without_on_entry()
        {
            var sut = CreateSut(ComplexSubsystem.State.D, new ComplexSubsystem());
            var events = sut.PlayEvents(EventCommandRequested(examples.complex_subsystem._ta, default, TestTime));
            var expected = EventCommandRequested(examples.complex_subsystem._cmd1, Literal.Create("toto"), TestTime);
            Check.That(events).Contains(expected);
        }
    }
}