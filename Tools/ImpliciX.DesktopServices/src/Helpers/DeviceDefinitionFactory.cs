using ImpliciX.DesktopServices.Services.Project;
using ImpliciX.Language;

namespace ImpliciX.DesktopServices.Helpers;

internal interface IDeviceDefinitionFactory
{
  IDeviceDefinition Create(string path, ApplicationDefinition appDef);
}

internal sealed class DeviceDefinitionFactory : IDeviceDefinitionFactory
{
  public IDeviceDefinition Create(string path, ApplicationDefinition appDef)
    => new DeviceDefinition(path, appDef);
}