using System;
using System.IO;
using ImpliciX.Designer.Features;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels;

public class SessionCommands
{
  private readonly Action _command;
  private readonly IFeatures _features;

  public SessionCommands(
    ISessionService.Session session,
    IFeatures features
  )
  {
    var sessionPath = session.Path;
    var sessionConnection = session.Connection;

    ContainConnection = !string.IsNullOrEmpty(sessionConnection);
    if (!string.IsNullOrEmpty(sessionPath))
    {
      ContainPath = true;
      IsNuget = Path.GetExtension(sessionPath).ToLower().EndsWith("nupkg");
      IsCsproj = Path.GetExtension(sessionPath).ToLower().EndsWith("csproj");
    }

    var loadFileText = "load " + Path.GetFileNameWithoutExtension(sessionPath);
    var reconnectToDeviceText = "reconnect to " + sessionConnection;
    Text =
      string.IsNullOrEmpty(sessionConnection)
        ? loadFileText
        : string.IsNullOrEmpty(sessionPath)
          ? reconnectToDeviceText
          : $"{loadFileText} and {reconnectToDeviceText}";

    _command = (sessionPath, sessionConnection) switch
    {
      ("", "") => () => { },
      (_, "") => () => { Window.LoadDeviceDefinition(sessionPath); },
      ("", _) => () => { Window.ConnectTo(sessionConnection); },
      (_, _) => () =>
      {
        Window.LoadDeviceDefinition(sessionPath);
        Window.ConnectTo(sessionConnection);
      }
    };
    _features = features;
  }

  public bool ContainConnection { get; }
  public bool ContainPath { get; }
  public string Text { get; }

  private IMainWindow Window => _features.Window;
  public bool IsNuget { get; }
  public bool IsCsproj { get; }

  public void Command()
  {
    _command();
  }
}
