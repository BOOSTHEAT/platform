using System;
using ImpliciX.Designer.ViewModels;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.Tests
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
                    .Body(fragment)
                    .Transitions
                .Define(State.B)
                    .Transitions
                .Define(State.C)
            
            ;
            // @formatter:on
        }

        public enum State
        {
            A, AA, AAA,
            B,
            C,
            AB,
            AAB
        }

        public static void Create(Action<BaseStateViewModel, BaseStateViewModel, DefinitionViewModel> addTransition, Action<bool, Fragment> fragment)
        {
            var ss = new IncludeFragment();
            ViewModelBuilder.Run(ss, addTransition, _ => { },fragment);
        }
    }

    public class TestFragment : FragmentDefinition<IncludeFragment.State>
    {
        public TestFragment()
        {
            var self = examples.test_fragment;
            var parent =  examples.include_fragment;
            var innerFragment = new TestInnerFragment();
            Fragment(self,parent)
            // @formatter:off
                .Initial(IncludeFragment.State.AA)
                .Define(IncludeFragment.State.AA)
                    .Body(innerFragment)
                .Transitions
                    .Define(IncludeFragment.State.AB)
            
            ;
            // @formatter:on
        }
    }

    public class TestInnerFragment : FragmentDefinition<IncludeFragment.State>
    {
        public TestInnerFragment()
        {
            var self = examples.test_inner_fragment;
            var parent =  examples.test_fragment;
            Fragment(self,parent)
            // @formatter:off
                .Initial(IncludeFragment.State.AAA)
                .Define(IncludeFragment.State.AAA)
                    .Transitions
                .Define(IncludeFragment.State.AAB)
            ;
            // @formatter:on
        }
    }
    
    public class examples : RootModelNode
    {
        public examples() : base(nameof(examples))
        {
        }

        public static include_fragment include_fragment => new include_fragment(new examples());
        public static test_fragment test_fragment => new test_fragment(new examples());
        public static test_inner_fragment test_inner_fragment => new test_inner_fragment(new examples());
    }

    public class include_fragment : SubSystemNode
    {
        public include_fragment(ModelNode parent) : base(nameof(include_fragment), parent)
        {
        }
        public PropertyUrn<SubsystemState> public_state => PropertyUrn<SubsystemState>.Build(Urn, nameof(public_state));
    }
    
    public class test_fragment : SubSystemNode
    {
        public test_fragment(ModelNode parent) : base(nameof(test_fragment), parent)
        {
        }
    }

    public class test_inner_fragment : SubSystemNode
    {
        public test_inner_fragment(ModelNode parent) : base(nameof(test_inner_fragment), parent)
        {
        }
    }
}