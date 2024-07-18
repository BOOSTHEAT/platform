using ImpliciX.Data.Api;

namespace ImpliciX.DesktopServices.Services;

internal sealed class RemoteDeviceDefinition : IRemoteDeviceDefinition
{
  public RemoteDeviceDefinition(MessagePrelude prelude)
  {
    Name = prelude.Name;
    Version = prelude.Version;
    Setup = prelude.Setup;
    Setups = prelude.Setups;
  }

  public string Name { get; }
  public string Version { get; }
  public string Setup { get; }
  public string[] Setups { get; }
}