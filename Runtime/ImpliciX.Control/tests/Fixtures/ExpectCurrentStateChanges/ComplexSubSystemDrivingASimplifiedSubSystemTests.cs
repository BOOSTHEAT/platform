using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectCurrentStateChanges
{
    [TestFixture]
    public class ComplexSubSystemDrivingASimplifiedSubSystemTests : SetupSubSystemTests
    {
        [Test]
        public void jump_test()
        {
            var complexSubSystem = CreateSut(ComplexSubsystem.State.A, new ComplexSubsystem());
            var simplifiedSubSystem = CreateSut(SimplifiedSubsystem.State.A, new SimplifiedSubsystem());
            simplifiedSubSystem.PlayEvents(EventCommandRequested(examples.simplified_subsystem._activate, default, TestTime));
            var outputCommands = complexSubSystem.PlayEvents(
                EventCommandRequested(examples.complex_subsystem._activate, default, TestTime),
                EventCommandRequested(examples.complex_subsystem._tab, default, TestTime),
                EventCommandRequested(examples.complex_subsystem._tb, default, TestTime));

            simplifiedSubSystem.PlayEvents(outputCommands);
            Check.That(simplifiedSubSystem.CurrentState).IsEqualTo(SimplifiedSubsystem.State.B);

            outputCommands =
                complexSubSystem.PlayEvents(EventCommandRequested(examples.complex_subsystem._tc, default, TestTime));
            simplifiedSubSystem.PlayEvents(outputCommands);

            Check.That(simplifiedSubSystem.CurrentState).IsEqualTo(SimplifiedSubsystem.State.NotANotB);
        }
    }
}