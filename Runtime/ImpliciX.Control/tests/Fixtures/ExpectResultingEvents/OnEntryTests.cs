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
    public class OnEntryTests : SubSystemTests
    {
        [Test]
        public void set_command_with_value()
        {
            var sut = CreateSut(ComplexSubsystem.State.C, new ComplexSubsystem());
            var events = sut.PlayEvents(EventCommandRequested(examples.complex_subsystem._activate, default, TestTime));
            var expected = EventPropertyChanged(examples.complex_subsystem.prop2, PowerSupply.Off, TestTime);
            Check.That(events).Contains(expected);
        }

    }
}