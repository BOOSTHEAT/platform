using System;
using System.Threading.Tasks;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class RemoteDeviceViewModel : ViewModelBase
{
  public RemoteDeviceViewModel(ILightConcierge concierge)
  {
    _concierge = concierge;
    _concierge.RemoteDevice.TargetSystem.Subscribe(ts =>
    {
      _targetSystem = ts;
      this.RaisePropertyChanged(nameof(BoardName));
      this.RaisePropertyChanged(nameof(OperatingSystem));
      this.RaisePropertyChanged(nameof(Architecture));
      UpdateSetupTool();
    });
    _concierge.RemoteDevice.DeviceDefinition.Subscribe(dd =>
    {
      _deviceDefinition = dd;
      this.RaisePropertyChanged(nameof(Name));
      this.RaisePropertyChanged(nameof(Version));
      this.RaisePropertyChanged(nameof(Setup));
      this.RaisePropertyChanged(nameof(Setups));
      NextSetup = Setup;
      UpdateSetupTool();
    });
  }

  private void UpdateSetupTool()
  {
    this.RaisePropertyChanged(nameof(CanChangeSetupForever));
    this.RaisePropertyChanged(nameof(CanChangeSetupUntilNextReboot));
    this.RaisePropertyChanged(nameof(CanChangeSetup));
    this.RaisePropertyChanged(nameof(NextSetup));
  }

  private readonly ILightConcierge _concierge;
  private ITargetSystem _targetSystem;
  private IRemoteDeviceDefinition _deviceDefinition;

  public bool CanChangeSetup => Setups.Length > 0 && (CanChangeSetupForever || CanChangeSetupUntilNextReboot);

  public string BoardName => _targetSystem?.Name ?? String.Empty;
  public string OperatingSystem => _targetSystem?.SystemInfo.Os.ToString() ?? String.Empty;
  public string Architecture => (_targetSystem?.SystemInfo.Architecture+" "+_targetSystem?.SystemInfo.Hardware).Trim();
  public bool CanChangeSetupForever =>  _targetSystem?.NewSetup.IsAvailable ?? false;
  public bool CanChangeSetupUntilNextReboot => _targetSystem?.NewTemporarySetup.IsAvailable ?? false;
  
  public string Name => _deviceDefinition?.Name ?? String.Empty;
  public string Version => _deviceDefinition?.Version ?? String.Empty;
  public string Setup => _deviceDefinition?.Setup ?? String.Empty;
  public string[] Setups => _deviceDefinition?.Setups ?? new string[]{};
  public string NextSetup { get; set; }
  
  public async Task ChangeSetupUntilNextReboot()
    => await ChangeSetup("until next reboot", _targetSystem.NewTemporarySetup);
  public async Task ChangeSetupForever()
    => await ChangeSetup("permanently", _targetSystem.NewSetup);

  public async Task ChangeSetup(string msg, ITargetSystemCapability cap)
  {
    var box = new IUser.Box
    {
      Title = "Change setup",
      Message = $"Do you want to change the application setup to {NextSetup} {msg}?",
      Icon = IUser.Icon.Stop,
      Buttons = IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No),
    };
    var choice = await _concierge.User.Show(box);
    if (choice != IUser.ChoiceType.Yes)
      return;
    await cap.Execute(NextSetup).AndWriteResultToConsole();
  }
}