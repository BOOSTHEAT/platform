using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.Designer.ViewModels
{
  public class BaseStateViewModel : ViewModelBase
  {
    public BaseStateViewModel(string name, int index, DefinitionViewModel definition, IEnumerable<BaseStateViewModel> children = null)
    {
      Name = name;
      Index = index;
      Definition = definition;
      Children = children?.ToArray() ?? new BaseStateViewModel[]{};
    }

    public string Name { get; private set; }
    public int Index { get; private set; }
    public BaseStateViewModel[] Children { get; private set; }
    public DefinitionViewModel Definition { get; private set; }

    public IEnumerable<BaseStateViewModel> Tree => Children.Concat(Children.SelectMany(c => c.Tree)).Prepend(this);
  }
}