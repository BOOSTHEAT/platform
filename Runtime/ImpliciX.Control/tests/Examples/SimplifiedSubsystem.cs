using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples
{
    public class SimplifiedSubsystem : SubSystemDefinition<SimplifiedSubsystem.State>
    {
        public enum State
        {
            A, B, NotANotB
        }

        public SimplifiedSubsystem()
        {
            var _jump = examples.simplified_subsystem._jump;
            Subsystem(examples.simplified_subsystem)
                .Initial(State.A)
                .Define(State.A)
                    .Transitions
                        .WhenMessage(_jump, NameOf(State.B)).Then(State.B)
                        .WhenMessage(_jump, NameOf(State.NotANotB)).Then(State.NotANotB)
                .Define(State.B)
                    .Transitions
                        .WhenMessage(_jump, NameOf(State.A)).Then(State.A)
                        .WhenMessage(_jump, NameOf(State.NotANotB)).Then(State.NotANotB)
                        .When(Condition.Is(examples.simplified_subsystem.presence,Presence.Disabled)).Then(State.NotANotB)
                .Define(State.NotANotB)
                    .Transitions
                        .WhenMessage(_jump, NameOf(State.B)).Then(State.B)
                        .WhenMessage(_jump, NameOf(State.A)).Then(State.A)
                ;
        }
    }
}