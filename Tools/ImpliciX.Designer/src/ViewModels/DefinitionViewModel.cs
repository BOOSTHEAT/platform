using System.Collections.Generic;

namespace ImpliciX.Designer.ViewModels
{
  public class DefinitionViewModel : ViewModelBase
  {
    public DefinitionViewModel( params DefinitionItemViewModel[] items)
    {
        Items = items;
    }

    public DefinitionViewModel(IEnumerable<DefinitionItemViewModel> items)
    {
        Items = items;
    }

    public IEnumerable<DefinitionItemViewModel> Items { get; private set; }
  }
}