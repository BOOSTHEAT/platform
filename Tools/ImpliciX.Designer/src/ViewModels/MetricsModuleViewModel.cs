using ImpliciX.Language.Metrics;

namespace ImpliciX.Designer.ViewModels;

public class MetricsModuleViewModel : NamedModel
{
  public MetricsModuleDefinition Definition { get; }

  public MetricsModuleViewModel(MetricsModuleDefinition definition) : base("Metrics")
  {
    Definition = definition;
  }
}