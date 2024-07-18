using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class NestedComposite : SubSystemDefinition<NestedComposite.State>
    {
        public NestedComposite()
        {
            Subsystem(examples.nested_composites)
                .Initial(State.A)
                .Define(State.A)
                    .Transitions
                        .WhenMessage(examples.nested_composites._tb).Then(State.B)
                .Define(State.B)
                    .OnState
                        .Set(examples.nested_composites._cmd1, examples.nested_composites.value)
                    .Define(State.Ba).AsInitialSubStateOf(State.B)
                        .OnState
                            .Set(examples.nested_composites._cmd2, examples.nested_composites.value)
                        .Define(State.Baa).AsInitialSubStateOf(State.Ba)
                            .Transitions
                                .WhenMessage(examples.nested_composites._tbab).Then(State.Bab)
                        .Define(State.Bab).AsSubStateOf(State.Ba)
                        .Transitions
                            .WhenMessage(examples.nested_composites._tbb).Then(State.Bb)
                    .Define(State.Bb).AsSubStateOf(State.B)
                        .Transitions
                            .WhenMessage(examples.nested_composites._tb).Then(State.B);
        }

        public enum State
        {
            A, B, Ba, Baa, Bab, Bb
        }
    }
}