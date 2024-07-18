using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Structures;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SharedKernel.FiniteStateMachine;

public class FSMDefinition<TState, TInput, TOutput>
{
    private readonly Tree<TState> _compositionTree;
    private readonly Dictionary<TState, StateDefinition<TState, TInput, TOutput>> _statesDefinition;
    private readonly Dictionary<TState, Func<TInput, TOutput[]>[]> _onStateFuncs;
    private readonly Dictionary<(TState, TState), (TState, Func<TInput, TOutput[]>[])> _transitionsDefinition;
    private readonly Dictionary<TState, TState[]> _statesComposition;
    private readonly Dictionary<TState, Transition<TState, TInput>[]> _definedTransitions;

    public FSMDefinition(StateDefinition<TState, TInput, TOutput>[] statesDefinitions,
        Transition<TState, TInput>[] transitions)
    {
        _statesDefinition = statesDefinitions.ToDictionary(s => s.Alias, s => s);
        ;
        _compositionTree = TreeBuilder<TState>.Build(_statesDefinition.Values, s => s.Alias, s => s.ParentState);
        _statesComposition = LoadStatesComposition();
        _definedTransitions = LoadTransitionsFrom(transitions);
        _transitionsDefinition = LoadTransitionsDefinition(transitions);
        _onStateFuncs = LoadOnStateFuncs();
    }

    public Transition<TState, TInput>[] TransitionsFrom(TState fromState) =>
        _definedTransitions.ContainsKey(fromState)
            ? _definedTransitions[fromState]
            : new Transition<TState, TInput>[0];

    public Func<TInput, TOutput[]>[] OnStateFunctions(TState state) => _onStateFuncs[state];

    public (TState nextState, Func<TInput, TOutput[]>[] funcs) Transition(TState fromState, TState toState) =>
        _transitionsDefinition.GetOrAdd((fromState, toState), _ => FindTransitionDefinition(fromState, toState));

    public TState[] StatesToEnter(TState ofState) => _statesComposition[ofState];

    public (TState finalStateToEnter, Func<TInput, TOutput[]>[] onEntryFuncs) EnterStateDefinition(
        TState stateToEnter, Option<TState> stateToExit) =>
        DefinitionForEnterState(stateToEnter, stateToExit);

    public bool IsSubState(TState parent, TState child)
    {
        return _compositionTree.SubNodesOf(parent).Contains(child);
    }

    private Dictionary<TState, Func<TInput, TOutput[]>[]> LoadOnStateFuncs()
    {
        var onStateFuncs = new Dictionary<TState, Func<TInput, TOutput[]>[]>();
        foreach (var state in _statesDefinition.Keys)
        {
            var ancestors = _compositionTree.AncestorsOf(state);
            var nodesToCompute = ancestors.Prepend(state).Reverse();
            var funcsOnState = nodesToCompute.SelectMany(s => _statesDefinition[s].OnStateFuncs).ToArray();
            onStateFuncs.Add(state, funcsOnState);
        }

        return onStateFuncs;
    }

    private Dictionary<TState, TState[]> LoadStatesComposition()
    {
        var statesComposition = new Dictionary<TState, TState[]>();
        foreach (var state in _statesDefinition.Keys)
        {
            var paths = _compositionTree.AncestorsOf(state).ToArray().Reverse().Concat(new[] {state}).ToArray();
            statesComposition.Add(state, paths);
        }

        return statesComposition;
    }

    private Dictionary<TState, Transition<TState, TInput>[]> LoadTransitionsFrom(
        Transition<TState, TInput>[] allTransitions) =>
        allTransitions.GroupBy(t => t.From)
            .ToDictionary(g => g.Key, g => g.ToArray());

    private Dictionary<(TState fromState, TState toState), (TState nextState, Func<TInput, TOutput[]>[] funcs)>
        LoadTransitionsDefinition(Transition<TState, TInput>[] allTransitions)
    {
        var transitionsDefinition =
            new Dictionary<(TState fromState, TState toState), (TState nextState, Func<TInput, TOutput[]>[] funcs
                )>();
        foreach (var (fromState, toState) in allTransitions.Select(t => (t.From, t.To)).Distinct())
        {
            var (nextState, funcs) = FindTransitionDefinition(fromState, toState);
            transitionsDefinition.Add((fromState, toState), (nextState, funcs));
        }

        return transitionsDefinition;
    }

    private (TState nextState, Func<TInput, TOutput[]>[] funcs) FindTransitionDefinition(TState fromState,
        TState toState)
    {
        var functionToExecuteOnExit = FindOnExitStateFunctions(fromState, toState);
        var (nextState, functionToExecuteOnEntry) = EnterStateDefinition(toState, fromState);
        var functionToExecute = functionToExecuteOnExit.Concat(functionToExecuteOnEntry).ToArray();
        return (nextState, functionToExecute);
    }

    private (TState finalStateToEnter, Func<TInput, TOutput[]>[] onEntryFuncs) DefinitionForEnterState(
        TState stateToEnter, Option<TState> stateToExit)
    {
        var successorsAndSelf = InitialStates(stateToEnter);
        var commonAncestor = _compositionTree.FirstCommonAncestor(stateToEnter, stateToExit.GetValue());

        var depthLimit = 0;

        if (!commonAncestor.IsRoot)
            depthLimit = _compositionTree.DepthOf(commonAncestor.Data.GetValue()) + 1;

        var ancestors = from stateExit in stateToExit
            let acs = _compositionTree.AncestorsOf(stateToEnter, (uint) depthLimit)
            select acs;
        var nodesToCompute = ancestors.GetValueOrDefault(new TState[] { }).Concat(successorsAndSelf).ToArray();
        var onEntryFuncs = nodesToCompute.SelectMany(s => _statesDefinition[s].OnEntryFuncs).ToArray();
        return (successorsAndSelf.Last(), onEntryFuncs);
    }

    private Func<TInput, TOutput[]>[] FindOnExitStateFunctions(TState fromState, TState toState)
    {
        var commonAncestor = _compositionTree.FirstCommonAncestor(fromState, toState);
        var depthLimit = 0;
        if (!commonAncestor.IsRoot)
            depthLimit = _compositionTree.DepthOf(commonAncestor.Data.GetValue()) + 1;
        var ancestors = _compositionTree.AncestorsOf(fromState, (uint) depthLimit).Prepend(fromState);
        var onExitFuncs = ancestors.SelectMany(s => _statesDefinition[s].OnExitFuncs).ToArray();
        return onExitFuncs;
    }

    private TState[] InitialStates(TState stateToEnter)
    {
        var statesToEnter = new List<TState>() {stateToEnter};
        var enterNode = _compositionTree.GetTreeNode(stateToEnter).GetValue();
        var nextState = NextInitialState(enterNode);
        while (nextState != null)
        {
            statesToEnter.Add(nextState.Data.GetValue());
            nextState = NextInitialState(nextState);
        }

        return statesToEnter.ToArray();

        TreeNode<TState> NextInitialState(TreeNode<TState> inputNode)
        {
            return inputNode.Children.FirstOrDefault(c => _statesDefinition[c.Data.GetValue()].IsInitialSubState);
        }
    }
}