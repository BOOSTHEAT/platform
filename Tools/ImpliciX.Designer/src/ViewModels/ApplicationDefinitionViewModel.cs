using System.Collections.Generic;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels;

public class ApplicationDefinitionViewModel : NamedModel
{
  public IDeviceDefinition DeviceDefinition { get; }

  public ApplicationDefinitionViewModel(IDeviceDefinition deviceDefinition) : base($"{deviceDefinition.Name} {deviceDefinition.Version}")
  {
    DeviceDefinition = deviceDefinition;
  }

  public override IEnumerable<string> CompanionUrns => DeviceDefinition.Urns.Keys;
}