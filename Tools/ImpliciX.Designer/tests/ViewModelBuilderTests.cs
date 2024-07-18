using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Control.Condition;

namespace ImpliciX.Designer.Tests
{
    [TestFixture]
    public class ViewModelBuilderTests
    {
        [Test]
        public void should_build_view_model()
        {
            Check.That(sut.Name).IsEqualTo("SomeSystem:SomeSubSystem");
        }

        [Test]
        public void should_build_view_model_with_all_edges()
        {
            var edges = sut.MyGraph.Edges.ToList();
            Check.That(edges).HasSize(7);

            Check.That(edges.Select(e => e.Label)).ContainsOnlyInstanceOfType(typeof(DefinitionViewModel));

            IEnumerable<(string, string)> LabelsTexts(int i) => DefinitionItems((DefinitionViewModel) edges[i].Label);

            Check.That(LabelsTexts(0)).IsEmpty();
            Check.That(LabelsTexts(1)).ContainsExactly(("🗣", "SomeSystem:SomeSubSystem:I tell you go to B2()"));
            Check.That(LabelsTexts(2)).ContainsExactly(("🗣", "SomeSystem:SomeSubSystem:I tell you go to C()"));
            Check.That(LabelsTexts(3)).ContainsExactly(("⏳⇉", "SomeSystem:SomeSubSystem:It's time to go to B"));
            Check.That(LabelsTexts(4)).ContainsExactly(("😺", "SomeSystem:SomeSubSystem[StateB]"));

            Check.That(edges[0].Tail).IsInstanceOf<InitialStateViewModel>();
            Check.That(edges[0].Head).IsEqualTo(edges[2].Tail);
            Check.That(edges[2].Head).IsEqualTo(edges[3].Tail);
            Check.That(edges[3].Head).IsEqualTo(edges[4].Tail);
            Check.That(edges[4].Head).IsEqualTo(edges[2].Tail);
        }


        [Test]
        public void should_build_view_model_with_all_nodes()
        {
            var nodes = sut.MyGraph.Edges.SelectMany(e => new object[] {e.Tail, e.Head}).Distinct().ToList();
            Check.That(nodes).HasSize(8);
            Check.That(nodes[0]).IsInstanceOf<InitialStateViewModel>();

            var standardNodes = nodes.Where(n => n is StateViewModel).Cast<StateViewModel>()
                .ToList();

            Check.That(standardNodes[0].Name).IsEqualTo("StateA");
            Check.That(DefinitionItems(standardNodes[0].Definition)).ContainsExactly(
                ("⇉⭘", "SomeSystem:SomeSubSystem:Action = Something"),
                ("⭗", "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration"),
                ("⭗", "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)"), 
                ("⥁", "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)")
            );

            Check.That(standardNodes[1].Name).IsEqualTo("StateB1");
            Check.That(DefinitionItems(standardNodes[1].Definition)).ContainsExactly(
                ("⇉⭘", "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration"));

            Check.That(standardNodes[2].Name).IsEqualTo("StateB2");
            Check.That(DefinitionItems(standardNodes[2].Definition)).ContainsExactly(
                ("⇉⭘", "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration"),
                ("⭘⇉", "SomeSystem:SomeSubSystem:Action = Something"));

            // Check.That(standardNodes[3].Name).IsEqualTo("StateC");
            Check.That(DefinitionItems(standardNodes[3].Definition)).ContainsExactly(
                ("⇉⭘", "SomeSystem:SomeSubSystem:Action = SomethingElse"),
                ("⇉⭘", "SomeSystem:SomeSubSystem:I tell you go to C"),
                ("⇉⏳", "SomeSystem:SomeSubSystem:It's time to go to B")
            );

            var compositeNodes = nodes.Where(n => n is CompositeStateViewModel)
                .Cast<CompositeStateViewModel>().ToList();

            Check.That(compositeNodes[0].Name).IsEqualTo("StateB");
            Check.That(DefinitionItems(compositeNodes[0].Definition)).ContainsExactly(
                ("⇉⭘", "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration"),
                ("⭗", "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)")
            );
        }

        [TestCase(Op.Lt, "OperandA < OperandB")]
        [TestCase(Op.Gt, "OperandA > OperandB")]
        [TestCase(Op.LtOrEqTo, "OperandA ≤ OperandB")]
        [TestCase(Op.GtOrEqTo, "OperandA ≥ OperandB")]
        [TestCase(Op.LtOrEqPlusEpsilon, "OperandA ≤ OperandB + OperandC")]
        [TestCase(Op.LtOrEqMinusEpsilon, "OperandA ≤ OperandB - OperandC")]
        [TestCase(Op.GtOrEqPlusEpsilon, "OperandA ≥ OperandB + OperandC")]
        [TestCase(Op.GtOrEqMinusEpsilon, "OperandA ≥ OperandB - OperandC")]
        [TestCase(Op.LtPlusEpsilon, "OperandA < OperandB + OperandC")]
        [TestCase(Op.LtMinusEpsilon, "OperandA < OperandB - OperandC")]
        [TestCase(Op.GtPlusEpsilon, "OperandA > OperandB + OperandC")]
        [TestCase(Op.GtMinusEpsilon, "OperandA > OperandB - OperandC")]
        [TestCase(Op.EqWithEpsilon, "|OperandA - OperandB| ≤ OperandC")]
        [TestCase(Op.EqWithTolerance, "|OperandA - OperandB| ≤ |OperandA| × OperandC")]
        [TestCase(Op.NotEqWithTolerance, "|OperandA - OperandB| > |OperandA| × OperandC")]
        [TestCase(Op.Is, "OperandA = OperandB")]
        [TestCase(Op.IsNot, "OperandA ≠ OperandB")]
        public void should_build_conditions_with_operators(Op @operator, string expected)
        {
            var conditionDefinition = new ConditionDefinition();
            conditionDefinition.Add(@operator, new Urn[] {"OperandA", "OperandB", "OperandC"});
            var result = ViewModelBuilder.BuildPartDescription(conditionDefinition.Parts[0]);
            Check.That(result).IsEqualTo(expected);
        }

        [Test]
        public void should_build_conditions_with_operator_any()
        {
            // Op @operator,string expected
            var conditionDefinition1 = new ConditionDefinition();
            conditionDefinition1.Add(Op.Is, new Urn[] {"OperandA", "OperandB"});
            var conditionDefinition2 = new ConditionDefinition();
            conditionDefinition2.Add(Op.Is, new Urn[] {"OperandC", "OperandD"});
            var conditionDefinitionAny = new ConditionDefinition();
            conditionDefinitionAny.Add(Op.Any, new Urn[] { }, new object[] {conditionDefinition1, conditionDefinition2});
            var result = ViewModelBuilder.BuildPartDescription(conditionDefinitionAny.Parts[0]);
            Check.That(result).IsEqualTo($"(OperandA = OperandB) {Environment.NewLine}                    OR {Environment.NewLine}                    (OperandC = OperandD)");
        }

        [Test]
        public void state_models_have_urns()
        {
            static DefinitionViewModel GetDefinition(object o) => o is BaseStateViewModel x ? x.Definition : o as DefinitionViewModel;
            var nodes = sut.MyGraph.Edges
                .SelectMany(e => new object[] {e.Tail, e.Head}).Distinct()
                .Select(GetDefinition).Where(x => x != null)
                .SelectMany(x => x.Items).ToArray();

            Check.That(nodes.Select(x => x.Description)).ContainsExactly(
                "SomeSystem:SomeSubSystem:Action = Something",
                "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration",
                "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)",
                "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)",
                "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration",
                "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration",
                "SomeSystem:SomeSubSystem:Action = Something",
                "SomeSystem:SomeSubSystem:Action = SomethingElse",
                "SomeSystem:SomeSubSystem:I tell you go to C",
                "SomeSystem:SomeSubSystem:It's time to go to B",
                "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration",
                "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)",
                "SomeSystem:SomeSubSystem:Action = SomeSystem:SomeSubSystem:SomeValueFromConfiguration",
                "SomeSystem:SomeSubSystem:Action = Polynomial1(SomeSystem:SomeSubSystem:MyFunction,SomeSystem:SomeSubSystem:MyInput)"
            );
            Check.That(GetUrnsAsOrderedStrings(nodes)).ContainsExactly(
                "SomeSystem:SomeSubSystem:Action",
                "SomeSystem:SomeSubSystem:I tell you go to C",
                "SomeSystem:SomeSubSystem:It's time to go to B",
                "SomeSystem:SomeSubSystem:MyFunction",
                "SomeSystem:SomeSubSystem:MyInput",
                "SomeSystem:SomeSubSystem:SomeValueFromConfiguration"
            );
        }

        [Test]
        public void transition_models_have_urns()
        {
            var edges = sut.MyGraph.Edges.Select(e => e.Label).Cast<DefinitionViewModel>().SelectMany(x => x.Items).ToArray();
            Check.That(edges.Select(x => x.Description)).ContainsExactly(
                "SomeSystem:SomeSubSystem:I tell you go to B2()",
                "SomeSystem:SomeSubSystem:I tell you go to C()",
                "SomeSystem:SomeSubSystem:It's time to go to B",
                "SomeSystem:SomeSubSystem[StateB]"
            );
            Check.That(GetUrnsAsOrderedStrings(edges)).ContainsExactly(
                "SomeSystem:SomeSubSystem:I tell you go to B2",
                "SomeSystem:SomeSubSystem:I tell you go to C",
                "SomeSystem:SomeSubSystem:It's time to go to B",
                "SomeSystem:SomeSubSystem:state"
            );
        }

        private static IOrderedEnumerable<string> GetUrnsAsOrderedStrings(DefinitionItemViewModel[] divms)
            => divms.SelectMany(x => x.Urns).Select(x => x.Value).Distinct().OrderBy(x => x);


        [Test]
        public void live_properties_use_subsystem_urns()
        {
            Check.That(sut.Urns).ContainsExactly(
            _action, Message_GotoB2, Message_GotoC, Timer_GotoB, _someFunctionDefinition,
            MyInput, _someValueFromConfiguration, _someSubSystem.state);
        }

        [Test]
        public void live_properties_use_subsystem_state_conditions()
        {
            var other = new SubSystemNode("OtherSubSystem", _rootSystem);
            var definition = new SubSystemDefinition<State>();
            definition.Subsystem(root.fakeSubsystem)
                .Initial(State.StateA)
                .Define(State.StateA)
                    .Transitions
                        .When(InState(other, State.StateB1)).Then(State.StateB)
                        .When(InState(other, State.StateB2)).Then(State.StateB)
                .Define(State.StateB);
            sut = new SubSystemViewModel(
                _someSubSystem.Urn,
                ILightConcierge.Create(),
                (addTransition, setApi, __) => ViewModelBuilder.Run(definition, addTransition, _ => { }, (b, s) => { }));
            Check.That(sut.Urns).ContainsExactly(
                other.state
            );
        }

        [Test]
        public void should_get_correct_relationship_between_fragment()
        {
            var definition = new IncludeFragment();
            var result = new Fragment() {RootSubsystemDefinitionId = definition.ID, AssociatedStates = ViewModelBuilder.GetRelationshipBetweenState(definition)};
            var expected = new Fragment()
            {
                RootSubsystemDefinitionId = "examples:include_fragment",
                AssociatedStates = new List<List<string>>()
                {
                    new List<string>() {"A", "AA", "AAA"},
                    new List<string>() {"A", "AA", "AAB"},
                    new List<string>() {"A", "AB"},
                    new List<string>() {"B"},
                    new List<string>() {"C"}
                }
            };

            Check.That(result.AssociatedStates).ContainsExactly(expected.AssociatedStates);
        }

        static IEnumerable<(string, string)> DefinitionItems(DefinitionViewModel dvm) =>
            dvm.Items.Select(i => (i.Symbol, i.Description));

        static IEnumerable<(string, string)> DefinitionItems(CompositeDefinitionViewModel dvm) =>
            dvm.Items.Select(i => (i.Symbol, i.Description));

        SubSystemViewModel sut;

        [SetUp]
        public void Setup()
        {
            var definition = new SubSystemDefinition<State>();
            definition.Subsystem(root.fakeSubsystem)
                .Initial(State.StateA)
                .Define(State.StateA)
                    .OnEntry
                        .Set(_action, Values.Something)
                    .OnState
                        .Set(_action, Polynomial1.Func, _someFunctionDefinition, MyInput)
                        .Set(_action, _someValueFromConfiguration)
                        .SetPeriodical(_action, Polynomial1.Func, _someFunctionDefinition, MyInput)
                    .Transitions
                        .WhenMessage(Message_GotoC).Then(State.StateC)
                .Define(State.StateC)
                    .OnEntry
                        .StartTimer(Timer_GotoB)
                        .Set(_action, Values.SomethingElse)
                        .Set(Message_GotoC)
                    .Transitions
                        .WhenTimeout(Timer_GotoB).Then(State.StateB)
                .Define(State.StateB)
                    .OnEntry
                        .Set(_action, _someValueFromConfiguration)
                    .OnState
                        .Set(_action, Polynomial1.Func, _someFunctionDefinition, MyInput)
                    .Transitions
                        .When(InState(_someSubSystem, State.StateB)).Then(State.StateA)
                .Define(State.StateB1).AsInitialSubStateOf(State.StateB)
                    .OnEntry
                        .Set(_action, _someValueFromConfiguration)
                    .Transitions
                        .WhenMessage(Message_GotoB2).Then(State.StateB2)
                .Define(State.StateB2).AsSubStateOf(State.StateB)
                    .OnEntry
                        .Set(_action, _someValueFromConfiguration)
                    .OnExit
                        .Set(_action, Values.Something);
            sut = new SubSystemViewModel(
                _someSubSystem.Urn,
                ILightConcierge.Create(),
                (addTransition, setApi, __) => ViewModelBuilder.Run(definition, addTransition, _ => { }, (b, s) => { }));
        }
        
        public class Polynomial1
        {
            public static FuncRef Func => new FuncRef(nameof(Polynomial1), () => Runner, xUrns=>xUrns);
            public static FunctionRun Runner =>
                (functionDefinition, xs) =>
                {
                    float result = 0;

                    foreach (var param in functionDefinition.Params)
                    {
                        var paramName = param.Key;
                        var hasParsedParam = int.TryParse(paramName.Substring(1), out int i);
                        result += param.Value * MathF.Pow(xs[0].value, i);
                    }

                    return result;
                };
        }


        public enum State
        {
            StateA,
            StateB,
            StateB1,
            StateB2,
            StateC
        }


        public static RootModelNode _rootSystem = new RootModelNode("SomeSystem");
        public static SubSystemNode _someSubSystem = new SubSystemNode("SomeSubSystem", _rootSystem);

        public static CommandUrn<NoArg> Message_GotoC =>
            CommandUrn<NoArg>.Build(_someSubSystem.Urn, "I tell you go to C");

        public static CommandUrn<NoArg> Message_GotoB2 =>
            CommandUrn<NoArg>.Build(_someSubSystem.Urn, "I tell you go to B2");

        public static PropertyUrn<Duration> Timer_GotoB =>
            PropertyUrn<Duration>.Build(_someSubSystem.Urn, "It's time to go to B");

        public static PropertyUrn<Values> _someValueFromConfiguration =>
            PropertyUrn<Values>.Build(_someSubSystem.Urn, "SomeValueFromConfiguration");

        public static PropertyUrn<FunctionDefinition> _someFunctionDefinition =>
            PropertyUrn<FunctionDefinition>.Build(_someSubSystem.Urn, "MyFunction");

        public static PropertyUrn<Values> MyInput => PropertyUrn<Values>.Build(_someSubSystem.Urn, "MyInput");

        public static CommandNode<Values> _action => CommandNode<Values>.Create("Action", _someSubSystem);

        public enum Values
        {
            Something,
            SomethingElse
        }
    }
}