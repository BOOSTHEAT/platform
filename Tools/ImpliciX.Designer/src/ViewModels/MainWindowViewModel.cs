using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImpliciX.Designer.Features;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class MainWindowViewModel : ViewModelBase, IMainWindow, IDisposable
{
  private readonly IFeatures _features;
  private IMainWindowContent _workspace;

  public MainWindowViewModel(IFeatures features)
  {
    _features = features;

    if (Environment.GetEnvironmentVariable("BH_DESIGNER_STYLE") == "Dock")
      Workspace = new MainWindowDockViewModel(_features);
    else
      Workspace = new MainWindowClassicViewModel(_features);

    LiveConnectViewModel = new LiveConnectViewModel(_features.Concierge);
    _features.CompleteInitialization(this);
  }
  public string Title => _features.Title;
  public MenuItemViewModel[] MenuItems => _features.MenuItems;

  public void Dispose()
  {
    _features.Dispose();
  }
  public LiveConnectViewModel LiveConnectViewModel { get; }
  public Action<string> SaveAsPdf { get; set; }
  public bool AutoConnect { get; set; }

  public IMainWindowContent Workspace
  {
    get => _workspace;
    private set => this.RaiseAndSetIfChanged(ref _workspace, value);
  }

  public async void SelectAndLoadDeviceDefinition()
  {
    var file = await _features.Concierge.User.OpenFile(new IUser.FileSelection
    {
      Title = "Load device project",
      Filters = _features.Filters,
      AllowMultiple = false
    });

    if (file.Choice != IUser.ChoiceType.Ok)
      return;
    LoadDeviceDefinition(file.Paths[0]);
  }

  public async void LoadDeviceDefinition(string path)
  {
    try
    {
      _features.Concierge.Applications.Load(path);
    }
    catch (Exception e)
    {
      await new Errors(_features.Concierge).Display(e);
    }
  }

  public async void ConnectTo(string connection)
  {
    try
    {
      if (LiveConnectViewModel.IsConnected)
        LiveConnectViewModel.Disconnect();
      LiveConnectViewModel.ConnectionString = connection;
      await LiveConnectViewModel.Connect();
    }
    catch (Exception e)
    {
      await new Errors(_features.Concierge).Display(e);
    }
  }

  public void Close()
  {
    _features.Concierge.RemoteDevice.Disconnect();
    _features.Concierge.Applications.UnLoad();
  }

  public IObservable<IEnumerable<string>> PreviousDeviceDefinitionPaths
    => _features.Concierge.Applications.PreviousPaths;

  public IEnumerable<string> LatestPreviousDeviceDefinitionPaths
    => _features.Concierge.Applications.LatestPreviousPaths;

  public void ExitApp() => Environment.Exit(0);

  public async void SelectOutputFileAndSaveAllDiagramsAsPdf()
  {
    var file = await _features.Concierge.User.SaveFile(new IUser.FileSelection
    {
      Title = "Save all diagrams as PDF",
      Filters = new List<IUser.FileSelectionFilter>
      {
        new() {Name = "PDF files (.pdf)", Extensions = new List<string> {"pdf"}},
        new() {Name = "All files", Extensions = new List<string> {"*"}}
      },
      InitialFileName = "subsystems.pdf"
    });

    if (file.Choice != IUser.ChoiceType.Ok)
      return;

    SaveAsPdf(file.Path);
  }

  public async Task OnOpened()
  {
    if (AutoConnect)
      await LiveConnectViewModel.Connect();

    // if (_features.ShallLoadOnStartup)
    //   SelectAndLoadDeviceDefinition();
  }
}
