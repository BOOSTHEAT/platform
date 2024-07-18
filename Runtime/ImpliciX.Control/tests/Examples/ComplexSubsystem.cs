using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples
{
    public class ComplexSubsystem : SubSystemDefinition<ComplexSubsystem.State>
    {
        public enum State
        {
            A,
            Aa,
            Ab,
            B,
            Ba,
            C,
            D
        }

        public ComplexSubsystem()
        {
            // @formatter:off
            Subsystem(examples.complex_subsystem)
                .Initial(State.A)
                .Define(State.A)
                    .OnEntry
                        .Set(examples.simplified_subsystem._jump, NameOf(SimplifiedSubsystem.State.A))
                    .OnExit
                        .Set(examples.simplified_subsystem._jump, NameOf(SimplifiedSubsystem.State.NotANotB))
                .Define(State.Aa).AsInitialSubStateOf(State.A)
                    .Transitions
                        .WhenMessage(examples.complex_subsystem._tab).Then(State.Ab)
                .Define(State.Ab).AsSubStateOf(State.A)
                    .Transitions
                        .WhenMessage(examples.complex_subsystem._tb).Then(State.B)
                .Define(State.B)
                    .OnEntry
                        .Set(examples.simplified_subsystem._jump, NameOf(SimplifiedSubsystem.State.B))
                    .OnExit
                        .Set(examples.simplified_subsystem._jump, NameOf(SimplifiedSubsystem.State.NotANotB))
                .Define(State.Ba).AsInitialSubStateOf(State.B)
                    .Transitions
                        .WhenMessage(examples.complex_subsystem._tc).Then(State.C)
                .Define(State.C)
                    .OnEntry
                        .Set(examples.complex_subsystem.prop2,PowerSupply.Off)
                        .Set(examples.complex_subsystem.prop3,PowerSupply.On)
                    .OnExit
                        .Set(examples.complex_subsystem.prop1, PowerSupply.Off)
                        .Set(examples.complex_subsystem._te, examples.complex_subsystem.prop2)
                        .Set(examples.complex_subsystem._tg)
                        .Set(examples.complex_subsystem.prop3, examples.complex_subsystem.prop2)
                    .Transitions
                        .WhenMessage(examples.complex_subsystem._td).Then(State.D)
                .Define(State.D)
                    .OnState
                        .Set(examples.complex_subsystem._te, examples.complex_subsystem.prop2)
                    .OnExit
                        .Set(examples.complex_subsystem._cmd1, Literal.Create("toto"))
                    .Transitions
                        .WhenMessage(examples.complex_subsystem._ta).Then(State.A)
                ;
            // @formatter:on
        }
    }
}