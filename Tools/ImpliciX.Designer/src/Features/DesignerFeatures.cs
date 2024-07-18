using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using DynamicData.Binding;
using ImpliciX.Data;
using ImpliciX.Designer.ViewModels;
using ImpliciX.Designer.ViewModels.Help;
using ImpliciX.Designer.ViewModels.LiveMenu;
using ImpliciX.Designer.ViewModels.ProjectMenu;
using ImpliciX.Designer.ViewModels.Tools;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.Features;

public class DesignerFeatures : ReactiveObject, IFeatures
{
  private readonly CompositeDisposable _compositeDisposable;
  private readonly IConcierge _concierge;
  private IMainWindow _window;

  public DesignerFeatures()
  {
    _concierge = IConcierge.Create(new User(Title));
    Home = new WelcomeViewModel(this);
    Filters = new List<IUser.FileSelectionFilter>
    {
      new() { Name = "Nuget (.nupkg)", Extensions = new List<string> { "nupkg" } },
      new() { Name = "Assemblies (.dll)", Extensions = new List<string> { "dll" } },
      new() { Name = "CsProj (.csproj)", Extensions = new List<string> { "csproj" } },
      new() { Name = "All files", Extensions = new List<string> { "*" } }
    };

    LiveMenu = new MenuItemViewModel
    {
      Text = "_Live!",
      StaticItems = new MenuItemViewModel[]
      {
        new BackupSettings(
          _concierge,
          "Backup user settings...",
          "user_settings.csv",
          () => Window?.Workspace?.System?.Device?.UserSettings
        ),
        new BackupSettings(
          _concierge,
          "Backup all settings...",
          "all_settings.csv",
          () => Window?.Workspace?.System?.Device?.AllSettings
        ),
        new ScenarioViewModel(_concierge),
        new MenuSeparatorViewModel(),
        IFeatures.TargetSystemMenuItem(
          _concierge,
          "Reset to factory settings",
          x => x?.SettingsReset,
          "This will restore factory settings and lose all equipment configuration.\nDo you really want to proceed?"
        ),
        new LiveUpdateSoftware(_concierge),
        IFeatures.TargetSystemMenuItem(
          _concierge,
          "Reset to factory software",
          x => x?.ResetToFactorySoftware,
          "This will restore factory software and lose all equipment data.\nDo you really want to proceed?"
        ),
        new BackupInfluxDB(_concierge),
        new LiveBackupSystemHistory(_concierge),
        LiveDownloadColdData.Menu(_concierge),
        new OpenSshTerminal(_concierge),
        new LiveSaveSystemDefinition(_concierge),
        IFeatures.TargetSystemMenuItem(
          _concierge,
          "Reboot the system",
          x => x?.SystemReboot,
          "Do you really want to proceed?"
        )
      }
    };

    ProjectMenu = new MenuItemViewModel
    {
      Text = "Project",
      IsEnabled = false,
      StaticItems = new MenuItemViewModel[]
      {
        new ProjectMakeViewModel(_concierge),
        new PackageCreationViewModel(_concierge),
        new RunGuiViewModel(_concierge)
        // TODO : 6731 : BuildWebHelpActionMenuViewModel out of menu, because docker run issue
        //new ProjectMenu.BuildWebHelpActionMenuViewModel(() => MainWindow, concierge)
      }
    };

    MenuItems = new[]
    {
      new MenuItemViewModel
      {
        Text = "_File",
        StaticItems = new[]
        {
          this.OpenDeviceDefinitionMenu(),
          this.CloseDeviceDefinitionMenu(),
          new MenuSeparatorViewModel(),
          new CommandViewModel(
            "Save all diagrams as PDF...",
            () => Window?.SelectOutputFileAndSaveAllDiagramsAsPdf()
          ),
          new MenuSeparatorViewModel(),
          new MenuItemViewModel
          {
            Text = "Identity",
            StaticItems = new IdentityViewModel(_concierge).Items.ToArray()
          },
          new MenuSeparatorViewModel(),
          new CommandViewModel(
            "Exit",
            () => Window?.ExitApp()
          )
        }
      },
      ProjectMenu,
      LiveMenu,
      new MenuItemViewModel
      {
        Text = "_Tools",
        StaticItems = new MenuItemViewModel[]
        {
          new DockerizedChronografViewModel(_concierge),
          new DockerizedGrafanaViewModel(_concierge),
          new DockerizedRedisCommanderViewModel(_concierge),
          new MenuSeparatorViewModel(),
          new DockerizedInfluxDbViewModel(_concierge),
          new DockerizedSystemHistoryViewModel(_concierge),
          new MenuSeparatorViewModel(),
          new XmlToCsViewModel(_concierge)
        }
      },
      new MenuItemViewModel
      {
        Text = "_Help",
        StaticItems = new MenuItemViewModel[]
        {
          new OpenUrlActionMenu(
            _concierge,
            "Online Help",
            "https://dev.azure.com/boostheat/ImpliciX/_wiki/wikis/ImpliciX.wiki/870/Designer"
          ),
          new MenuSeparatorViewModel(),
          new About(
            _concierge,
            Title
          )
        }
      }
    };

    _compositeDisposable = new CompositeDisposable();
  }

  public IUser User => _concierge.User;
  public MenuItemViewModel ProjectMenu { get; }
  public MenuItemViewModel LiveMenu { get; }

  public IMainWindow Window
  {
    get => _window;
    set => this.RaiseAndSetIfChanged(
      ref _window,
      value
    );
  }

  public ILightConcierge Concierge => _concierge;
  public string Title => "ImpliciX Designer";
  public NamedModel Home { get; }
  public MenuItemViewModel[] MenuItems { get; }
  public List<IUser.FileSelectionFilter> Filters { get; }
  public bool ShallLoadOnStartup => _concierge.ProjectsManager.LatestProject.IsNone;

  public void CompleteInitialization(
    IMainWindow window
  )
  {
    Window = window;
    var projectMenuOnlyAvailableWhenDeviceBuilt = _concierge.Applications.Devices
      .Subscribe(d => ProjectMenu.IsEnabled = d != null);
    _compositeDisposable.Add(projectMenuOnlyAvailableWhenDeviceBuilt);

    var liveMenuOnlyAvailableWhenConnected = Window.LiveConnectViewModel
      .WhenValueChanged(lcvm => lcvm.IsConnected)
      .BindTo(
        this,
        x => x.LiveMenu.IsEnabled
      );
    _compositeDisposable.Add(liveMenuOnlyAvailableWhenConnected);
  }

  public void RegisterUserOn(TopLevel topLevel)
  {
    ((User)User).RegisterOn(topLevel);
  }

  public void Dispose()
  {
    _compositeDisposable.Dispose();
  }
}
