using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;

namespace ImpliciX.Designer.ViewModels;

public class ControlCommandModuleViewModel : NamedModel
{
  public ISubSystemDefinition[] Definitions { get; }

  public ControlCommandModuleViewModel(IEnumerable<ISubSystemDefinition> definitions) : base("Control & Command")
  {
    Definitions = definitions.ToArray();
  }
}