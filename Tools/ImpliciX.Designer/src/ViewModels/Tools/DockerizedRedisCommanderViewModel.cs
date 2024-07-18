using System;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.Tools;

public class DockerizedRedisCommanderViewModel : ActionMenuViewModel<IConcierge>
{
  private const string ContainerName = "bhRedisCommander7711";
  private const string ImageName = "rediscommander/redis-commander:latest";
  private const string LocalPort = "7711";

  public DockerizedRedisCommanderViewModel(
    IConcierge concierge
  ) : base(concierge)
  {
    Text = "Open Redis Commander...";
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
            true,
            IDockerService.DefinePortBindings(("8081/tcp", "0.0.0.0", LocalPort))
          );
          await Concierge.OperatingSystem.OpenUrl($"http://127.0.0.1:{LocalPort}");
        }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      }
    );
  }
}
