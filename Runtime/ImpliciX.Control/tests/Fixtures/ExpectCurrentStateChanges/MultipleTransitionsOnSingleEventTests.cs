using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectCurrentStateChanges
{
    [TestFixture]
    public class MultipleTransitionsOnSingleEventTests : SetupSubSystemTests
    {
        [Test]
        public void transition_condition_is_satisfied_when_enter_on_state()
        {
            var simplifiedSubSystem = CreateSut(SimplifiedSubsystem.State.A, new SimplifiedSubsystem());
            WithProperties((examples.simplified_subsystem.presence, Presence.Disabled));

            var outputCommands = simplifiedSubSystem.PlayEvents(
                EventCommandRequested(examples.simplified_subsystem._activate, default, TestTime),
                EventCommandRequested(examples.simplified_subsystem._jump, SubsystemState.Create(SimplifiedSubsystem.State.B), TestTime));

            simplifiedSubSystem.PlayEvents(outputCommands);
            Check.That(simplifiedSubSystem.CurrentState).IsEqualTo(SimplifiedSubsystem.State.NotANotB);
        }
    }
}