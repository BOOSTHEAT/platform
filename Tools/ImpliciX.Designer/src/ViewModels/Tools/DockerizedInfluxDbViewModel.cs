using System;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.Tools;

public class DockerizedInfluxDbViewModel : ActionMenuViewModel<IConcierge>
{
  private const string ContainerName = "bhInfluxDB7712";
  private const string ImageName = "influxdb:1.8";
  private const string LocalPort = "7712";

  public DockerizedInfluxDbViewModel(
    IConcierge concierge
  ) : base(concierge)
  {
    Text = "Load InfluxDB...";
  }

  public override async void Open()
  {
    var folder = await Concierge.User.OpenFolder(
      new IUser.FileSelection
      {
        Title = "Load InfluxDB"
      }
    );
    if (folder.Choice != IUser.ChoiceType.Ok)
      return;
    await BusyWhile(
      async () =>
      {
        try
        {
          var docker = Concierge.Docker;
          await docker.Stop(ContainerName);
          await docker.Pull(ImageName);
          await docker.Launch(
            ImageName,
            ContainerName,
            true,
            IDockerService.DefinePortBindings(("8086/tcp", "0.0.0.0", LocalPort)),
            new []
            {
              ("/var/lib/influxdb", folderName: folder.Path)
            }
          );
        }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      }
    );
  }
}
