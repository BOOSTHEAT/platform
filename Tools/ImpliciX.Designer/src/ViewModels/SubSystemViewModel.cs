using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using AvaloniaGraphControl;
using DynamicData;
using ImpliciX.Data.Api;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class SubSystemViewModel : NamedModel, IKnowMyUniqueView
{
  public static Func<ISubSystemDefinition, SubSystemViewModel> Factory(ILightConcierge concierge)
  {
    return d => new SubSystemViewModel(
      d.ID,
      concierge,
      (addTransition, setApi, fragment)
        => ViewModelBuilder.Run(d, addTransition, setApi, fragment)
    );
  }

  public SubSystemViewModel(string name, ILightConcierge concierge,
    Action<Action<BaseStateViewModel, BaseStateViewModel, DefinitionViewModel>, Action<DefinitionViewModel>,
      Action<bool, Fragment>> initialize) :
    base(name)
  {
    initialize(
      (BaseStateViewModel s1, BaseStateViewModel s2, DefinitionViewModel definition) =>
        MyGraph.Edges.Add(new Edge(s1, s2, definition)),
      always => { Always = always; },
      (isFragment, fragmentRelationship) =>
      {
        IsFragment = isFragment;
        Fragment = fragmentRelationship;
      }
    );
    LeafStatesAndOutgoingTransitions = (
      from edge in MyGraph.Edges
      let origin = edge.Tail as BaseStateViewModel
      from state in origin.Tree
      where state is StateViewModel
      let connections = edge.Label as DefinitionViewModel
      from connection in connections.Items
      group connection by state
      into connectionsByState
      select connectionsByState
    ).ToDictionary(
      g => (StateViewModel)g.Key,
      g => g.Distinct().ToArray() as IEnumerable<DefinitionItemViewModel>);
    InitializeGraph(name);
    DefineLiveBehavior(name, concierge, IsFragment, Fragment);
    ShowTransitions = false;
    Urns = FindAllURNs().ToArray();
  }

  public override IEnumerable<string> CompanionUrns => Urns.Select(u => u.Value);

  public IEnumerable<Urn> Urns { get; }
  public Fragment Fragment { get; set; }

  public bool IsFragment { get; set; }

  public DefinitionViewModel Always { get; private set; }
  public Graph MyGraph { get; private set; } = new Graph();
  public Control View { get; set; }
  private IReadOnlyDictionary<string, StateViewModel> _leafStates;

  public IReadOnlyDictionary<StateViewModel, IEnumerable<DefinitionItemViewModel>> LeafStatesAndOutgoingTransitions
  {
    get;
  }


  private void InitializeGraph(string name)
  {
    _transitionTexts = MyGraph.Edges
      .Select(e => e.Label).Cast<DefinitionViewModel>()
      .Select(e => e.Items).Cast<DefinitionItemViewModel[]>()
      .SelectMany(e => e)
      .Where(item => item.IsTransition)
      .Distinct().ToList();
    _leafStates = MyGraph.Edges
      .SelectMany(e => new object[] { e.Head, e.Tail })
      .Where(s => s is StateViewModel).Cast<StateViewModel>().Distinct().ToDictionary(s => s.Name, s => s);
    var compositeStates = MyGraph.Edges
      .SelectMany(e => new object[] { e.Head, e.Tail })
      .Where(s => s is CompositeStateViewModel).Cast<CompositeStateViewModel>().Distinct().ToList();
    foreach (var compositeState in compositeStates)
    {
      MyGraph.Parent[compositeState.Definition] = compositeState;
      foreach (var child in compositeState.Children)
      {
        MyGraph.Parent[child] = compositeState;
        if (child is InitialStateViewModel)
        {
          MyGraph.Edges.Add(new InvisibleEdgeViewModel(compositeState.Definition, child));
        }
      }
    }

    MyGraph.Orientation = Graph.Orientations.Vertical;

    static int Score(object o) => o switch
    {
      CompositeDefinitionViewModel x => 300,
      BaseStateViewModel x => 100 - x.Index,
      _ => 0
    };

    MyGraph.VerticalOrder = (s1, s2) => Score(s2) - Score(s1);
  }

  private void DefineLiveBehavior(string name, ILightConcierge concierge, bool isFragment, Fragment fragment)
  {
    var app = concierge.RemoteDevice;
    var session = concierge.Session;

    void SendCommand(object raw)
    {
      var (urn, arg) = ((string, object))raw;
      var message = WebsocketApiV2.CommandMessage.WithParameter(urn, arg?.ToString()).ToJson();
      app.Send(message);
    }

    void Activate(string stateName, bool isActive)
    {
      var associatedStates = isFragment && fragment.AssociatedStates.Any(c => c.Contains(stateName))
        ? fragment.AssociatedStates.Single(c => c.Contains(stateName))
        : new List<string>() { stateName };

      foreach (var associatedState in associatedStates)
      {
        if (_leafStates.ContainsKey(associatedState))
        {
          var state = _leafStates[associatedState];
          state.IsActive = isActive;
          foreach (var transition in LeafStatesAndOutgoingTransitions.GetValueOrDefault(state,
                     Array.Empty<DefinitionItemViewModel>()))
            transition.DefineUserAction(isActive, SendCommand);
        }
      }
    }

    var subsystemState = name + ":state";
    if (isFragment)
    {
      subsystemState = fragment.RootSubsystemDefinitionId + ":state";
    }

    session.Properties
      .Connect()
      .Filter(property => property.Urn == subsystemState)
      .Subscribe(changeSet =>
      {
        var change = changeSet.Single();
        switch (change.Reason)
        {
          case ChangeReason.Add:
            Activate(change.Current.Value, true);
            break;
          case ChangeReason.Update:
            Activate(change.Previous.Value.Value, false);
            Activate(change.Current.Value, true);
            break;
          case ChangeReason.Remove:
            Activate(change.Current.Value, false);
            break;
        }
      });
  }

  private IEnumerable<Urn> FindAllURNs()
  {
    static DefinitionViewModel GetDefinition(object o) => o is BaseStateViewModel x
      ? x.Definition
      : o as DefinitionViewModel;

    var nodes = MyGraph.Edges
      .SelectMany(e => new object[] { e.Tail, e.Head }).Distinct()
      .Select(GetDefinition).Where(x => x != null);
    var edges = MyGraph.Edges.Select(e => e.Label).Cast<DefinitionViewModel>();
    var urns = nodes
      .Concat(edges)
      .SelectMany(x => x.Items)
      .SelectMany(x => x.Urns)
      .DistinctBy(x => x.Value)
      .OrderBy(x => x.Value);
    return urns;
  }

  private double zoom = 1;

  public double Zoom
  {
    get => zoom;
    set
    {
      AutoZoom = false;
      this.RaiseAndSetIfChanged(ref zoom, value);
    }
  }

  public void SetAutoZoom() => AutoZoom = true;

  private bool autozoom = true;

  public bool AutoZoom
  {
    get => autozoom;
    set
    {
      this.RaiseAndSetIfChanged(ref autozoom, value);
      ComputeZoom();
    }
  }

  public Avalonia.Rect VisibleBounds
  {
    set
    {
      visibleSize = value.Size;
      ComputeZoom();
    }
  }

  public Avalonia.Rect IdealBounds
  {
    set
    {
      idealSize = value.Size;
      ComputeZoom();
    }
  }

  private void ComputeZoom()
  {
    if (!AutoZoom || !visibleSize.HasValue || !idealSize.HasValue)
      return;
    zoom = Math.Min(1.0,
      0.99 * Math.Min(visibleSize.Value.Width / idealSize.Value.Width,
        visibleSize.Value.Height / idealSize.Value.Height));
    this.RaisePropertyChanged("Zoom");
  }

  private Avalonia.Size? visibleSize;
  private Avalonia.Size? idealSize;
  private List<DefinitionItemViewModel> _transitionTexts;

  private bool _showTransitions;

  public bool ShowTransitions
  {
    get => _showTransitions;
    set
    {
      AutoZoom = false;
      this.RaiseAndSetIfChanged(ref _showTransitions, value);
      _transitionTexts.ForEach(text => text.IsVisible = ShowTransitions);
    }
  }
}
