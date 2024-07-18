using System;
using System.Collections.Generic;
using System.Linq;
using AvaloniaGraphControl;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SharedKernel.DocTools
{
  public class FSMViewModel
  {
    string Name { get; }
    Graph MyGraph { get; }
  }
  
  public class FSMViewModel<TState> : FSMViewModel
    where TState : Enum
  {
    public FSMViewModel(FSM<TState, DomainEvent, DomainEvent> fsm)
    {
      var definition = fsm.GetPrivatePropertyValue<FSMDefinition<TState, DomainEvent, DomainEvent>>("Definition");
      var statesDefinition = definition.GetPrivateFieldValue<Dictionary<TState, StateDefinition<TState, DomainEvent, DomainEvent>>>("_statesDefinition");
      var transitions = definition
        .GetPrivateFieldValue<Dictionary<TState, Transition<TState, DomainEvent>[]>>("_definedTransitions")
        .SelectMany(kv => kv.Value);
      var composition = definition.GetPrivateFieldValue<Dictionary<TState, TState[]>>("_statesComposition");
      
      var composites = composition.Values.Select(v => v.Take(v.Length - 1)).SelectMany(v => v).Distinct().ToArray();
      var leaves = composition.Keys.Except(composites).ToArray();
      var nodes = composites.Select(c => CompositeStateViewModel.Create(statesDefinition[c]))
        .Cast<StateViewModel>()
        .Concat(leaves.Select(l => LeafStateViewModel.Create(statesDefinition[l])))
        .ToDictionary(n => n.Name, n => n);
      var edges = transitions.Select(t =>
        new Edge(
          nodes[t.From.ToString()!], 
          nodes[t.To.ToString()!],
          TransitionViewModel.Create(t)));
      foreach (var edge in edges)
        MyGraph.Edges.Add(edge);
      foreach (var node in nodes)
      {
        var svm = node.Value;
        if (svm.ParentName.IsSome)
        {
          var compositeNode = nodes[svm.ParentName.GetValue()];
          MyGraph.Parent[svm] = compositeNode;
          if (svm.IsInitialSubState)
          {
            var entryPoint = new EntryPointViewModel();
            MyGraph.Edges.Add(new Edge(entryPoint,svm));
            MyGraph.Parent[entryPoint] = compositeNode;
          }
        }

      }
    }

    public Graph MyGraph { get; } = new Graph();
    public string Name => typeof(TState).FullName!.Split('.')[^2];
  }
}