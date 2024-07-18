using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class IncludeFragment : SubSystemDefinition<IncludeFragment.State>
    {
        public IncludeFragment()
        {
            var self = examples.include_fragment;
            var fragment = new TestFragment();
            Subsystem(self)
            // @formatter:off
                .Initial(State.A)
                .Define(State.A)
                    .Body(fragment);
            // @formatter:on
        }

        public enum State
        {
            A, Af, Aif
        }
    }

    public class TestFragment : FragmentDefinition<IncludeFragment.State>
    {
        public TestFragment()
        {
            var self = examples.test_fragment;
            var parent = examples.include_fragment;
            var innerFragment = new TestInnerFragment();
            Fragment(self,parent)
            // @formatter:off
                .Initial(IncludeFragment.State.Af)
                .Define(IncludeFragment.State.Af)
                    .Body(innerFragment);
            // @formatter:on
        }
    }

    public class TestInnerFragment : FragmentDefinition<IncludeFragment.State>
    {
        public TestInnerFragment()
        {
            var self = examples.test_inner_fragment;
            var parent = examples.test_fragment;
            Fragment(self,parent)
            // @formatter:off
                .Initial(IncludeFragment.State.Aif)
                .Define(IncludeFragment.State.Aif);
            // @formatter:on
        }
    }
}