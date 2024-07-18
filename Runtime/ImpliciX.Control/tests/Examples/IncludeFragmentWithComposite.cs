using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class IncludeFragmentWithComposite : SubSystemDefinition<IncludeFragmentWithComposite.State>
    {
        public IncludeFragmentWithComposite()
        {
            var self = examples.include_fragment;
            var fragment = new TestFragmentWithComposite();
            Subsystem(self)
            // @formatter:off
                .Initial(State.A)
                .Define(State.A)
                    .Body(fragment);
            // @formatter:on
        }

        public enum State
        {
            A,
            B,
            C,
            D
        }
    }

    public class TestFragmentWithComposite : FragmentDefinition<IncludeFragmentWithComposite.State>
    {
        public TestFragmentWithComposite()
        {
            var self = examples.test_fragment;
            var parent = examples.include_fragment;
            Fragment(self, parent)
            // @formatter:off
                .Initial(IncludeFragmentWithComposite.State.B)
                .Define(IncludeFragmentWithComposite.State.B)
                    .Transitions
                        .WhenMessage(examples.include_fragment._toBc).Then(IncludeFragmentWithComposite.State.C)
                .Define(IncludeFragmentWithComposite.State.C)
                    .Transitions
                .Define(IncludeFragmentWithComposite.State.D).AsInitialSubStateOf(IncludeFragmentWithComposite.State.C);
            

            // @formatter:on
        }
    }
}