using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImpliciX.Designer.ViewModels;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer
{
    public static class ViewModelBuilder
    {
        public static void Run(object subSystemDefinition, Action<BaseStateViewModel, BaseStateViewModel, DefinitionViewModel> addTransition, Action<DefinitionViewModel> setAlways,
            Action<bool, Fragment> fragment)
        {
            var stateAndTriggerTypes =
                FindBaseType(subSystemDefinition.GetType(), t => t.GenericTypeArguments.Length == 1)
                    ?.GenericTypeArguments;
            var navigator = typeof(Navigator<>).MakeGenericType(stateAndTriggerTypes);
            try
            {
                navigator.GetMethod(nameof(Navigator<Enum>.Generate), BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new[] {addTransition, setAlways, fragment, subSystemDefinition});
            }
            catch (Exception)
            {
                Console.WriteLine($"Error during the generation of {subSystemDefinition.GetType()}");
                throw;
            }
        }

        private static Type FindBaseType(Type t, Predicate<Type> condition) =>
            (t == null || condition(t)) ? t : FindBaseType(t.BaseType, condition);


        class Navigator<S> : DslNavigator<S, DefinitionItemViewModel, DefinitionItemViewModel> where S : Enum
        {
            public Navigator()
            {
                DefineSection(s => s.OnEntry)
                    .DescribeOperation(p => p._setsValues,
                        x => DefinitionItemViewModel.CreateStateEntry(
                            x._urn.Value + ((x._value is NoArg)
                                ? string.Empty
                                : $" = {x._value}"), x._urn))
                    .DescribeOperation(p => p._setsWithProperties,
                        x => DefinitionItemViewModel.CreateStateEntry(
                            x._urn.Value + " = " + x._propertyUrn.Value, x._urn, x._propertyUrn))
                    .DescribeOperation(p => p._setsWithComputations,
                        x => DefinitionItemViewModel.CreateStateEntry(
                            $"{x._urnToSet.Value} = {x._funcRef.Name}({x._funcDefUrn.Value},{x._xUrns.Aggregate((m, n) => $"{m},{n}")})"
                            
                            /*
                             TODO: implement custom formatter for functions
                             
                            x.FuncId == FuncId.Identity
                                ? $"{x._urnToSet.Value} = {x._xUrns[0]}"
                                : $"{x._urnToSet.Value} = {x.FuncId}({x._funcDefUrn.Value},{x._xUrns.Aggregate((m, n) => $"{m},{n}")})" */,
                            x._xUrns.Append(x._funcDefUrn).Append(x._urnToSet).ToArray()))
                    .DescribeOperation(p => p._startTimers,
                        x => DefinitionItemViewModel.CreateStartTimer(x.Value, x));

                DefineSection(s => s.OnExit)
                    .DescribeOperation(p => p._setsValues,
                        x => DefinitionItemViewModel.CreateStateExit(
                            x._urn.Value + ((x._value is NoArg)
                                ? string.Empty
                                : $" = {x._value}"), x._urn))
                    .DescribeOperation(p => p._setsWithProperty,
                        x => DefinitionItemViewModel.CreateStateExit(
                            x._urn.Value + " = " + x._propertyUrn.Value, x._urn, x._propertyUrn));

                DefineSection(s => s.OnState, s=> s.Always)
                    .DescribeOperation(p => p._setWithProperties,
                        x => DefinitionItemViewModel.CreateStateDuring(
                            x._urn.Value + " = " + x._propertyUrn.Value,x._urn, x._propertyUrn))
                    .DescribeOperation(p => p._setWithComputations,
                        x => DefinitionItemViewModel.CreateStateDuring(
                            $"{x._urnToSet.Value} = {x._funcRef.Name}({x._funcDefUrn.Value},{x._xUrns.Aggregate((m, n) => $"{m},{n}")})"
                            /*
                            x.FuncId == FuncId.Identity
                                ? $"{x._urnToSet.Value} = {x._xUrns[0]}"
                                : $"{x._urnToSet.Value} = {x.FuncId}({x._funcDefUrn.Value},{x._xUrns.Aggregate((m, n) => $"{m},{n}")})" */,
                            x._xUrns.Append(x._funcDefUrn).Append(x._urnToSet).ToArray()))
                    .DescribeOperation(p => p._setPeriodical,
                        x => DefinitionItemViewModel.CreatePeriodical(
                            $"{x._urnToSet.Value} = {x._funcRef.Name}({x._funcDefUrn.Value},{x._xUrns.Aggregate((m, n) => $"{m},{n}")})"
                            /*
                            x.FuncId == FuncId.Identity
                                ? $"{x._urnToSet.Value} = {x._xUrns[0]}"
                                : $"{x._urnToSet.Value} = {x.FuncId}({x._funcDefUrn.Value},{x._xUrns.Aggregate((m, n) => $"{m},{n}")})" */,
                            x._xUrns.Append(x._funcDefUrn).Append(x._urnToSet).ToArray()))
                    .DescribeOperation(always => always._setWithConditions.Values,
                        set =>
                        {
                            string DisplayValue(object o) => o.GetType().IsEnum ? $"{o} ({Convert.ToInt32(o)})" : o.ToString();
                            var otherwise = set._withs.Single(c => c._isOtherwise);
                            var otherwiseValue = otherwise._isValueUrn ? (string)otherwise._valueUrn : DisplayValue(otherwise._value);
                            var result = DefinitionItemViewModel.CreateStateDuring(set._setUrn + set._withs.Where(c => !c._isOtherwise).Aggregate("",
                                    (acc, with) =>
                                    {
                                        if (with._isValueUrn)
                                        {
                                            return acc + Environment.NewLine + $"        → {with._valueUrn} ? {BuildConditionDescription(with._conditionDefinition)}";
                                        }

                                        return acc + Environment.NewLine + $"        → {DisplayValue(with._value)} ? {BuildConditionDescription(with._conditionDefinition)}";
                                    }) + $"{Environment.NewLine}        → {otherwiseValue} ? Otherwise  {Environment.NewLine}",
                                set._withs.Where(w => w._isValueUrn).Select(w => w._valueUrn).Append(set._setUrn).ToArray()
                            );

                            return result;
                        });

                DefineSection(s => s.Transitions)
                    .DescribeTransition(t => t._whenMessages, t => t._state,
                        x => DefinitionItemViewModel.CreateTransitionMessage(
                            x._urn + ((x._value == null) ? string.Empty : $"({x._value})"), true,
                            (x._urn.ToString(), x._value), x._urn))
                    .DescribeTransition(t => t._whenTimeouts, t => t._state,
                        x => DefinitionItemViewModel.CreateTransitionTimeout(x._timerUrn, x._timerUrn))
                    .DescribeTransition(t => t._whenConditions, t => t._target,
                        x => DefinitionItemViewModel.CreateTransitionCondition(
                            BuildConditionDescription(x.Definition), GetAllStrUrnsIn(x.Definition), GetAllUrnsIn(x.Definition).ToArray()));
            }

            public static void Generate(
                Action<BaseStateViewModel, BaseStateViewModel, DefinitionViewModel> addTransition, Action<DefinitionViewModel> defineGlobalOperations, Action<bool, Fragment> fragment,
                SubSystemDefinition<S> subsystemDefinition)
            {
                var dsl = new Navigator<S>();

                var stateDefinitions = dsl.GetStates(subsystemDefinition);
                if (subsystemDefinition._stateDefinitions.Any(c => c.Value._fragment != null || subsystemDefinition.IsFragment))
                {
                    var id = subsystemDefinition.ParentId != null ? subsystemDefinition.ParentId : subsystemDefinition.ID;
                    fragment(true, new Fragment() {RootSubsystemDefinitionId = id, AssociatedStates = GetRelationshipBetweenState(subsystemDefinition)});
                }

                int Level(Define<S> sd) =>
                    sd._parentState.Match(() => 0, parentState => Level(stateDefinitions[parentState.ToString()]) + 1);

                defineGlobalOperations(new DefinitionViewModel(dsl.GetGlobalOperations(subsystemDefinition)));
                var states = new Dictionary<Define<S>, BaseStateViewModel>();
                foreach (var stateDefinition in stateDefinitions.OrderByDescending(sd => Level(sd.Value)))
                {
                    var name = stateDefinition.Key;
                    var definition = stateDefinition.Value;
                    var index = Convert.ToInt32(definition._stateToConfigure);
                    var childrenDefinitions = stateDefinitions.Values.Where(t =>
                        t._parentState.IsSome && t._parentState.GetValue().Equals(definition._stateToConfigure)).ToArray();
                    states[definition] = childrenDefinitions.Any()
                        ? new CompositeStateViewModel(name, index,
                            new CompositeDefinitionViewModel(dsl.GetOperationsIn(definition).ToArray()),
                            childrenDefinitions.Select(cd => states[cd]).Prepend(new InitialStateViewModel()).ToArray())
                        : (BaseStateViewModel) new StateViewModel(name, index,
                            new DefinitionViewModel(dsl.GetOperationsIn(definition).ToArray()));
                }

                addTransition(new InitialStateViewModel(),
                    states[stateDefinitions[subsystemDefinition.InitialState.ToString()]], new DefinitionViewModel());

                foreach (var state in states)
                {
                    var definition = state.Key;
                    var origin = states[definition];
                    var transitions = dsl.GetTransitionsFrom(definition, stateDefinitions);
                    foreach (var destination in transitions)
                        addTransition(origin, states[destination.Key],
                            new DefinitionViewModel(destination.Value.ToArray()));
                    if (state.Value is CompositeStateViewModel csvm)
                    {
                        var initialState = stateDefinitions.First(sd =>
                            sd.Value._isInitialSubState &&
                            sd.Value._parentState.IsSome &&
                            sd.Value._parentState.GetValue().Equals(definition._stateToConfigure));
                        addTransition(csvm.Children.First(), states[stateDefinitions[initialState.Key]],
                            new DefinitionViewModel());
                    }
                }
            }
        }


        private static IEnumerable<Urn> GetAllUrnsIn(ConditionDefinition conditionDefinition) =>
            conditionDefinition.Parts
                .SelectMany(part => part.@operator switch
                {
                    Op.InState => part.operandsByUrn.Select(urn => Urn.BuildUrn(Urn.Deconstruct(urn.Value).Append("state").ToArray())),
                    Op.Any => part.operandsByValue.Cast<ConditionDefinition>().SelectMany(GetAllUrnsIn),
                    _ => part.operandsByUrn
                });
        private static IEnumerable<string> GetAllStrUrnsIn(ConditionDefinition conditionDefinition) =>
            conditionDefinition.Parts
                .SelectMany(part => part.@operator switch
                {
                    Op.InState => part.operandsByUrn.Select(urn => urn.Value+":state"),
                    Op.Any => part.operandsByValue.Cast<ConditionDefinition>().SelectMany(GetAllStrUrnsIn),
                    _ => part.operandsByUrn.Select(urn => urn.Value)
                });

        public static string BuildConditionDescription(ConditionDefinition conditionDefinition)
        {
            return conditionDefinition.Parts
                .Select(BuildPartDescription)
                .Aggregate((f, s) => string.Join($"{Environment.NewLine}                    AND ", f, s));
        }

        public static string BuildPartDescription((Op @operator, Urn[] operandsByUrn, object[] operandsByValue) part)
        {
            var (@operator, operandsByUrn, operandsByValue) = part;
            var operands = operandsByUrn.Concat(operandsByValue).ToArray();

            switch (@operator)
            {
                case Op.Gt:
                    return $"{operands[0]} > {operands[1]}";
                case Op.Lt:
                    return $"{operands[0]} < {operands[1]}";
                case Op.EqWithEpsilon:
                    return $"|{operands[0]} - {operands[1]}| ≤ {operands[2]}";
                case Op.EqWithTolerance:
                    return $"|{operands[0]} - {operands[1]}| ≤ |{operands[0]}| × {operands[2]}";
                case Op.NotEqWithTolerance:
                    return $"|{operands[0]} - {operands[1]}| > |{operands[0]}| × {operands[2]}";
                case Op.GtPlusEpsilon:
                    return $"{operands[0]} > {operands[1]} + {operands[2]}";
                case Op.GtMinusEpsilon:
                    return $"{operands[0]} > {operands[1]} - {operands[2]}";
                case Op.LtPlusEpsilon:
                    return $"{operands[0]} < {operands[1]} + {operands[2]}";
                case Op.LtMinusEpsilon:
                    return $"{operands[0]} < {operands[1]} - {operands[2]}";
                case Op.GtOrEqPlusEpsilon:
                    return $"{operands[0]} ≥ {operands[1]} + {operands[2]}";
                case Op.GtOrEqMinusEpsilon:
                    return $"{operands[0]} ≥ {operands[1]} - {operands[2]}";
                case Op.LtOrEqPlusEpsilon:
                    return $"{operands[0]} ≤ {operands[1]} + {operands[2]}";
                case Op.LtOrEqMinusEpsilon:
                    return $"{operands[0]} ≤ {operands[1]} - {operands[2]}";
                case Op.GtOrEqTo:
                    return $"{operands[0]} ≥ {operands[1]}";
                case Op.LtOrEqTo:
                    return $"{operands[0]} ≤ {operands[1]}";
                case Op.Is:
                    return $"{operands[0]} = {operands[1]}";
                case Op.IsNot:
                    return $"{operands[0]} ≠ {operands[1]}";
                case Op.InState:
                    return $"{operands[0]}[{operands[1]}]";
                case Op.Any:
                    var operand1 = BuildConditionDescription((ConditionDefinition) operandsByValue[0]);
                    var operand2 = BuildConditionDescription((ConditionDefinition) operandsByValue[1]);
                    return $"({operand1}) {Environment.NewLine}                    OR {Environment.NewLine}                    ({operand2})";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static List<List<string>> GetRelationshipBetweenState<TState>(SubSystemDefinition<TState> ssdParent, List<string> parentStates = null) where TState : Enum
        {
            var result = new List<List<string>>();
            parentStates ??= new List<string>();
            foreach (var define in ssdParent._stateDefinitions.Values)
            {
                var parentStatesWithCurrentState = new List<string>(parentStates) {define._stateToConfigure.ToString()};
                if (define._fragment != null)
                {
                    result.AddRange(GetRelationshipBetweenState(define._fragment, parentStatesWithCurrentState));
                }
                else
                {
                    result.Add(parentStatesWithCurrentState);
                }
            }

            return result;
        }
    }

    public class Fragment
    {
        public string RootSubsystemDefinitionId { get; set; }
        public List<List<string>> AssociatedStates { get; set; } = new List<List<string>>();
    }
}