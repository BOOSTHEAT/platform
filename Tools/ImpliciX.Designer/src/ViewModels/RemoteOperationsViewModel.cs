using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels;

public class RemoteOperationsViewModel : DockableViewModel
{
  public LivePropertiesViewModel LiveProperties { get; }
  public RemoteDeviceViewModel RemoteDevice { get; }
  public CommandsViewModel Commands { get; }

  public RemoteOperationsViewModel(ILightConcierge concierge)
  {
    LiveProperties = new LivePropertiesViewModel(concierge);
    RemoteDevice = new RemoteDeviceViewModel(concierge);
    Commands = new CommandsViewModel(concierge);
  }
}