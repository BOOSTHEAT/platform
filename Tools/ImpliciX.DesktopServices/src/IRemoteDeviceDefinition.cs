namespace ImpliciX.DesktopServices;

public interface IRemoteDeviceDefinition
{
  string Name { get; }
  string Version { get; }
  string Setup { get; }
  string[] Setups { get; }
}