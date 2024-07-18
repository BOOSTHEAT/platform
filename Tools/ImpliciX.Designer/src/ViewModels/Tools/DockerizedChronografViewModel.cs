using System;
using System.Linq;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.Tools;

public class DockerizedChronografViewModel : ActionMenuViewModel<IConcierge>
{
  private const string ContainerName = "bhChronograf7710";
  private const string ImageName = "chronograf:latest";
  private const string LocalPort = "7710";

  public DockerizedChronografViewModel(
    IConcierge concierge
  ) : base(concierge)
  {
    Text = "Open Chronograf...";
  }

  public override async void Open()
  {
    await BusyWhile(
      async () =>
      {
        try
        {
          var docker = Concierge.Docker;
          await docker.Pull(ImageName);
          await docker.Launch(
            ImageName,
            ContainerName,
            false,
            IDockerService.DefinePortBindings(("8888/tcp", "0.0.0.0", LocalPort))
          );
          var webAddress = $"http://127.0.0.1:{LocalPort}";
          var additionalInfo =
            Concierge.RemoteDevice.LocalIPAddresses.Any()
              ? $"\n\nDepending on your Docker install, you can use one of the following IP Addresses\nto configure the Chronograph connection to the currently connected device:\n{string.Join(", ", Concierge.RemoteDevice.LocalIPAddresses)}"
              : "";
          var def = new IUser.Box
          {
            Title = "Chronograph",
            Message =
              $"Chronograph is running inside a Docker container.\nA web page will now open at the following web address: {webAddress}{additionalInfo}",
            Icon = IUser.Icon.Info,
            Buttons = IUser.StandardButtons(IUser.ChoiceType.Ok)
          };
          await Concierge.User.Show(def);
          await Concierge.OperatingSystem.OpenUrl(webAddress);
        }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      }
    );
  }
}
