using System;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.ReactiveUI;
using Dock.Model.ReactiveUI.Controls;
using ImpliciX.Designer.Features;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels
{
  public class MainWindowDockViewModel : ViewModelBase, IMainWindowContent
  {
    public MainWindowDockViewModel(IFeatures features)
    {
      var concierge = features.Concierge;
      var home = new []
      {
        new NamedTree(features.Home)
      };
      System = new SystemViewModel(concierge, home, SubSystemViewModel.Factory(concierge));
      RemoteOperations = new RemoteOperationsViewModel(concierge);
      System.SelectModel.Subscribe(model =>
        {
          if(model is SubSystemViewModel ss)
            AddSubSystem(ss);
        },
        ex => { },
        () => { });
      Console = new ConsoleViewModel(concierge);
      _f = new Factory();
      _documentDock = new DocumentDock
      {
        Id = "DocumentsPane",
        Title = "DocumentsPane",
        Proportion = double.NaN,
        ActiveDockable = null,
        VisibleDockables = _f.CreateList<IDockable>()
      };
      var mainLayout = new ProportionalDock
      {
        Id = "MainLayout",
        Title = "MainLayout",
        Proportion = double.NaN,
        Orientation = Orientation.Horizontal,
        ActiveDockable = null,
        VisibleDockables = _f.CreateList<IDockable>(
          new ToolDock
          {
            Id = "LeftPane",
            Title = "LeftPane",
            Proportion = double.NaN,
            Alignment = Alignment.Left,
            GripMode = GripMode.Visible,
            ActiveDockable = System,
            VisibleDockables = _f.CreateList<IDockable>(System)
          },
          new ProportionalDockSplitter()
          {
            Id = "LeftSplitter",
            Title = "LeftSplitter"
          },
          new ProportionalDock
          {
            Id = "CentralPane",
            Title = "CentralPane",
            Proportion = double.NaN,
            Orientation = Orientation.Vertical,
            ActiveDockable = null,
            VisibleDockables = _f.CreateList<IDockable>(
              _documentDock,
              new ProportionalDockSplitter()
              {
                Id = "CentralSplitter",
                Title = "CentralSplitter"
              },
              new ToolDock
              {
                Id = "BottomPane",
                Title = "BottomPane",
                Proportion = 0.3,
                Alignment = Alignment.Bottom,
                GripMode = GripMode.Visible,
                ActiveDockable = Console,
                VisibleDockables = _f.CreateList<IDockable>(Console)
              }
              )
          },
          new ProportionalDockSplitter()
          {
            Id = "RightSplitter",
            Title = "RightSplitter"
          },
          new ToolDock
          {
            Id = "RightPane",
            Title = "RightPane",
            Proportion = double.NaN,
            Alignment = Alignment.Right,
            GripMode = GripMode.Visible,
            ActiveDockable = RemoteOperations,
            VisibleDockables = _f.CreateList<IDockable>(RemoteOperations)
          }
        )
      };
      var root = _f.CreateRootDock();
      root.Id = "Root";
      root.Title = "Root";
      root.ActiveDockable = mainLayout;
      root.DefaultDockable = mainLayout;
      root.VisibleDockables = _f.CreateList<IDockable>(mainLayout);
      
      _f.InitLayout(root);
      _f.SetActiveDockable(_documentDock);
      
      DockLayout = root;
    }

    private IDock _dockLayout;    
    public IDock DockLayout
    {
      get => _dockLayout;
      private set => this.RaiseAndSetIfChanged(ref _dockLayout, value);
    }

    private void AddSubSystem(SubSystemViewModel subSystem)
    {
      _f.AddDockable(_documentDock, subSystem);
      _f.SetActiveDockable(subSystem);
      _f.SetFocusedDockable(_documentDock, subSystem);
    }

    public SystemViewModel System { get; }
    public RemoteOperationsViewModel RemoteOperations { get; }
    public ConsoleViewModel Console { get; }
    private readonly IDocumentDock _documentDock;
    private readonly Factory _f;

  }
}