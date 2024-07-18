using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.Tools;

public class DockerizedGrafanaViewModel : ActionMenuViewModel<IConcierge>
{
  private const string ImageName = "docker.io/grafana/grafana-oss:latest";
  private const string ContainerName = "bhGrafana7714";
  private const string LocalPort = "7714";

  public DockerizedGrafanaViewModel(
    IConcierge concierge
  ) : base(concierge)
  {
    Text = "Open Grafana...";
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
            IDockerService.DefinePortBindings(("3000/tcp", "0.0.0.0", LocalPort))
            ,
            new []
            {
              ( "/etc/grafana/provisioning/datasources/datasource.yml:ro", GrafanaDatasourcesFilePath)
            }
            ,
            null,
            new []
            {
              ("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin") ,
              ("GF_AUTH_ANONYMOUS_ENABLED", "true") ,
              ("GF_INSTALL_PLUGINS", "grafana-clock-panel, grafana-influxdb-flux-datasource, simpod-json-datasource")
            }
          );
          var webAddress = $"http://127.0.0.1:{LocalPort}";
          var additionalInfo =
            Concierge.RemoteDevice.LocalIPAddresses.Any()
              ? $"\n\nDepending on your Docker install, you can use one of the following IP Addresses\nto configure the Grafana connection to the currently connected device:\n{string.Join(", ", Concierge.RemoteDevice.LocalIPAddresses)}"
              : "";
          var def = new IUser.Box
          {
            Title = "Grafana",
            Message =
              $"Grafana is running inside a Docker container.\nA web page will now open at the following web address: {webAddress}{additionalInfo}",
            Icon = IUser.Icon.Info,
            Buttons = IUser.StandardButtons(IUser.ChoiceType.Ok)
          };
          await Concierge.User.Show(def);
          await Concierge.OperatingSystem.OpenUrl(webAddress);
        }

        catch (Exception e)
        {
          Console.WriteLine(e);
          await Errors.Display(e);
        }
      }
    );
  }

  public static string GrafanaDatasourcesFilePath
  {
    get
    {
      var baseFolder = Directory.GetParent(Assembly.GetEntryAssembly()!.Location)!.FullName;
      var datasource = Path.Combine(baseFolder,"ViewModels","Tools","Grafana","grafana-datasources.yml");
      return datasource;
    }
  }
}
