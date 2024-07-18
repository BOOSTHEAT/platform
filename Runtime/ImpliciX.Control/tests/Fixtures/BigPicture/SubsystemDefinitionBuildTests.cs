using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Control.Tests.Fixtures.BigPicture
{
    [TestFixture]
    public class SubsystemDefinitionBuildTests : SetupSubSystemTests
    {
        private enum StateTestEnum
        {
            A,
            Aa,
            B,
            C
        }

        private class SubsystemDefinitionBuildTestNode : SubSystemNode
        {
            public SubsystemDefinitionBuildTestNode(ModelNode parent) : base(nameof(SubsystemDefinitionBuildTestNode),
                parent)
            {
            }
        }

        private class SubsystemDefinitionBuildFragmentTestNode : SubSystemNode
        {
            public SubsystemDefinitionBuildFragmentTestNode(ModelNode parent) : base(
                nameof(SubsystemDefinitionBuildFragmentTestNode), parent)
            {
            }
        }

        private class SubsystemDefinitionBuildTestFragmentWithFragmentB : FragmentDefinition<StateTestEnum>
        {
            public SubsystemDefinitionBuildTestFragmentWithFragmentB()
            {
                Fragment(new SubsystemDefinitionBuildFragmentTestNode(new examples()),
                        new SubsystemDefinitionBuildFragmentTestNode(new examples()))
                    .Initial(StateTestEnum.B)
                    .Define(StateTestEnum.B);
            }
        }

        private class SubsystemDefinitionBuildTestFragmentWithBAndCInAFragment : FragmentDefinition<StateTestEnum>
        {
            public SubsystemDefinitionBuildTestFragmentWithBAndCInAFragment()
            {
                Fragment(new SubsystemDefinitionBuildFragmentTestNode(new examples()),
                        new SubsystemDefinitionBuildFragmentTestNode(new examples()))
                    .Initial(StateTestEnum.B)
                    .Define(StateTestEnum.B).Transitions
                    .Define(StateTestEnum.C).AsInitialSubStateOf(StateTestEnum.B);
            }
        }

        private class SubsystemDefinitionBuildTestWithFragmentC : FragmentDefinition<StateTestEnum>
        {
            public SubsystemDefinitionBuildTestWithFragmentC()
            {
                Fragment(new SubsystemDefinitionBuildFragmentTestNode(new examples()),
                        new SubsystemDefinitionBuildFragmentTestNode(new examples()))
                    .Initial(StateTestEnum.C)
                    .Define(StateTestEnum.C);
            }
        }

        private class SubsystemDefinitionBuildTestFragmentWitFragmentBAndFragmentC : FragmentDefinition<StateTestEnum>
        {
            public SubsystemDefinitionBuildTestFragmentWitFragmentBAndFragmentC()
            {
                var fragmentAndFragmentC = new SubsystemDefinitionBuildTestWithFragmentC();
                Fragment(new SubsystemDefinitionBuildFragmentTestNode(new examples()),
                        new SubsystemDefinitionBuildFragmentTestNode(new examples()))
                    .Initial(StateTestEnum.B)
                    .Define(StateTestEnum.B)
                    .Body(fragmentAndFragmentC);
            }
        }

        [Test]
        public void should_compute_composite_state()
        {
            var sut = new SubSystemDefinition<StateTestEnum>();
            var fragmentWitFragmentBAndFragmentC = new SubsystemDefinitionBuildTestFragmentWitFragmentBAndFragmentC();
            sut.Subsystem(new SubsystemDefinitionBuildTestNode(new examples()))
                .Initial(StateTestEnum.Aa)
                .Define(StateTestEnum.Aa).Transitions
                .Define(StateTestEnum.A).AsInitialSubStateOf(StateTestEnum.Aa)
                .Body(fragmentWitFragmentBAndFragmentC);

            DefinitionProcessing.AddMetaDataToDefinition(sut);

            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.Aa]._isLeaf).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.A]._isLeaf).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.B]._isLeaf).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.C]._isLeaf).IsTrue();
        }

        [Test]
        public void should_compute_composite_within_fragments()
        {
            var sut = new SubSystemDefinition<StateTestEnum>();
            var fragmentWithFragmentBAndC = new SubsystemDefinitionBuildTestFragmentWithBAndCInAFragment();
            sut.Subsystem(new SubsystemDefinitionBuildTestNode(new examples()))
                .Initial(StateTestEnum.Aa)
                .Define(StateTestEnum.Aa).Transitions
                .Define(StateTestEnum.A).AsInitialSubStateOf(StateTestEnum.Aa)
                .Body(fragmentWithFragmentBAndC);

            DefinitionProcessing.AddMetaDataToDefinition(sut);

            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.Aa]._isLeaf).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.Aa]._isInitialSubState).IsTrue();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.Aa]._parentState).IsEqualTo(Option<StateTestEnum>.None());
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.A]._isLeaf).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.A]._isInitialSubState).IsTrue();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.A]._parentState).IsEqualTo(Option<StateTestEnum>.Some(StateTestEnum.Aa));
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.B]._isLeaf).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.B]._isInitialSubState).IsTrue();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.B]._parentState).IsEqualTo(Option<StateTestEnum>.Some(StateTestEnum.A));
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.C]._isLeaf).IsTrue();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.C]._isInitialSubState).IsTrue();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.C]._parentState).IsEqualTo(Option<StateTestEnum>.Some(StateTestEnum.B));
        }

        [Test]
        public void should_compute_fragment()
        {
            var sut = new SubSystemDefinition<StateTestEnum>();
            var fragmentWithFragmentB = new SubsystemDefinitionBuildTestFragmentWithFragmentB();
            sut.Subsystem(new SubsystemDefinitionBuildTestNode(new examples()))
                .Define(StateTestEnum.A)
                .Body(fragmentWithFragmentB);

            DefinitionProcessing.AddMetaDataToDefinition(sut);

            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.B]._isInitialSubState).IsTrue();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.B]._parentState).IsEqualTo(Option<StateTestEnum>.Some(StateTestEnum.A));
        }

        [Test]
        public void should_compute_fragment_and_preserve_other_state()
        {
            var sut = new SubSystemDefinition<StateTestEnum>();
            var fragmentWithFragmentB = new SubsystemDefinitionBuildTestFragmentWithFragmentB();
            sut.Subsystem(new SubsystemDefinitionBuildTestNode(new examples()))
                .Initial(StateTestEnum.Aa)
                .Define(StateTestEnum.Aa).Transitions
                .Define(StateTestEnum.A)
                .Body(fragmentWithFragmentB);

            DefinitionProcessing.AddMetaDataToDefinition(sut);

            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.A]._isInitialSubState).IsFalse();
            Check.That(sut.StateDefinitionsFlattened[StateTestEnum.A]._parentState).IsEqualTo(Option<StateTestEnum>.None());
        }

        [Test]
        public void should_compute_unfolded_state_definition_with_fragment()
        {
            var sut = new SubSystemDefinition<StateTestEnum>();
            var fragmentWithFragmentB = new SubsystemDefinitionBuildTestFragmentWitFragmentBAndFragmentC();
            sut.Subsystem(new SubsystemDefinitionBuildTestNode(new examples()))
                .Define(StateTestEnum.A)
                .Body(fragmentWithFragmentB);

            DefinitionProcessing.AddMetaDataToDefinition(sut);

            Check.That(sut.StateDefinitionsFlattened.ContainsKey(StateTestEnum.A)).IsTrue();
            Check.That(sut.StateDefinitionsFlattened.ContainsKey(StateTestEnum.B)).IsTrue();
            Check.That(sut.StateDefinitionsFlattened.ContainsKey(StateTestEnum.C)).IsTrue();
        }
    }
}