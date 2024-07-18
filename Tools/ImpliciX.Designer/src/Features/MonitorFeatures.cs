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
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.Features;

public class MonitorFeatures : ReactiveObject, IFeatures
{
  private readonly CompositeDisposable _compositeDisposable;
  private IMainWindow _window;

  public MonitorFeatures()
  {
    Concierge = ILightConcierge.Create(new User(Title));
    Concierge = WasmConcierge.Create(new User(Title));
    //Concierge = ILightConcierge.Create(new User(Title));
    Home = new WelcomeViewModel(this);
    Filters = new List<IUser.FileSelectionFilter>
    {
      new() { Name = "Nuget (.nupkg)", Extensions = new List<string> { "nupkg" } }
    };

    LiveMenu = new MenuItemViewModel
    {
      Text = "_Live!",
      StaticItems = new MenuItemViewModel[]
      {
        new BackupSettings(Concierge, "Backup user settings...",
          "user_settings.csv", () => _window?.Workspace?.System?.Device?.UserSettings),
        new BackupSettings(Concierge, "Backup all settings...",
          "all_settings.csv", () => _window?.Workspace?.System?.Device?.AllSettings),
        new ScenarioViewModel(Concierge),
        new MenuSeparatorViewModel(),
        IFeatures.TargetSystemMenuItem(Concierge,
          "Reset to factory settings", x => x?.SettingsReset,
          "This will restore factory settings and lose all equipment configuration.\nDo you really want to proceed?"
        ),
        new LiveUpdateSoftware(Concierge),
        IFeatures.TargetSystemMenuItem(Concierge,
          "Reset to factory software", x => x?.ResetToFactorySoftware,
          "This will restore factory software and lose all equipment data.\nDo you really want to proceed?"
        ),
        new BackupInfluxDB(Concierge),
        new LiveBackupSystemHistory(Concierge),
        LiveDownloadColdData.Menu(Concierge),
        new OpenSshTerminal(Concierge),
        new LiveSaveSystemDefinition(Concierge),
        IFeatures.TargetSystemMenuItem(Concierge,
          "Reboot the system", x => x?.SystemReboot,
          "Do you really want to proceed?"
        )
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
          new CommandViewModel("Save all diagrams as PDF...", () => _window?.SelectOutputFileAndSaveAllDiagramsAsPdf()),
          new MenuSeparatorViewModel(),
          new MenuItemViewModel
          {
            Text = "Identity",
            StaticItems = new IdentityViewModel(Concierge).Items.ToArray()
          },
          new MenuSeparatorViewModel(),
          new CommandViewModel("Exit", () => _window?.ExitApp())
        }
      },
      LiveMenu,
      new MenuItemViewModel
      {
        Text = "_Help",
        StaticItems = new MenuItemViewModel[]
        {
          new About(Concierge, Title)
        }
      }
    };

    _compositeDisposable = new CompositeDisposable();
  }

  public IUser User => (User)Concierge.User;
  public MenuItemViewModel LiveMenu { get; }
  public ILightConcierge Concierge { get; }
  public string Title => "ImpliciX Monitor";
  public NamedModel Home { get; }
  public MenuItemViewModel[] MenuItems { get; }
  public List<IUser.FileSelectionFilter> Filters { get; }
  public bool ShallLoadOnStartup => Concierge.Applications.LatestDevice.IsNone;

  public IMainWindow Window
  {
    get => _window;
    set => this.RaiseAndSetIfChanged(ref _window, value);
  }

  public void CompleteInitialization(IMainWindow window)
  {
    Window = window;
    var liveMenuOnlyAvailableWhenConnected = _window.LiveConnectViewModel
      .WhenValueChanged(lcvm => lcvm.IsConnected)
      .BindTo(this, x => x.LiveMenu.IsEnabled);
    _compositeDisposable.Add(liveMenuOnlyAvailableWhenConnected);
  }

  public void RegisterUserOn(TopLevel topLevel)
  {
    Console.WriteLine("\ud83d\udd51 MonitorFeatures.RegisterUserOn");
    ((User)User).RegisterOn(topLevel);
    Console.WriteLine("\u2713 MonitorFeatures.RegisterUserOn");
  }

  public void Dispose()
  {
    _compositeDisposable.Dispose();
  }
}
