using System;
using System.Linq;
using ImpliciX.Designer.Features;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels
{
  public class MainWindowClassicViewModel : ViewModelBase, IMainWindowContent
  {
    public MainWindowClassicViewModel(IFeatures features)
    {
      var concierge = features.Concierge;
      var home = new []
      {
        new NamedTree(features.Home)
      };
      System = new SystemViewModel(concierge, home, SubSystemViewModel.Factory(concierge));
      RemoteOperations = new RemoteOperationsViewModel(concierge);
      System.SelectModel.Subscribe(
        Set,
        ex => { },
        () => { }
        );
      Console = new ConsoleViewModel(concierge);
      Set(home.First().Parent);

      void Set(NamedModel model)
      {
        CurrentModel = model;
        RemoteOperations.LiveProperties.ContextURNs = model.CompanionUrns;
      }
    }

    public RemoteOperationsViewModel RemoteOperations { get; }
    public SystemViewModel System { get; }
    public ConsoleViewModel Console { get; }

    public NamedModel CurrentModel
    {
      get => _currentModel;
      private set => this.RaiseAndSetIfChanged(ref _currentModel, value);
    }
    private NamedModel _currentModel;
  }
}