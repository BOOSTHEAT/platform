using System.Collections.Generic;
using System.Linq;
using Dock.Model.ReactiveUI.Controls;

namespace ImpliciX.Designer.ViewModels
{
  public class NamedModel : Document
  {
    public NamedModel(string name)
    {
      Name = name;
      Title = name;
    }

    public string Name { get; private set; }
    public NamedModel Parent { get; internal set; }

    public string DisplayName => Parent != null && Name.StartsWith(Parent.Name) ? Name.Remove(0,Parent.Name.Length) : Name;
    
    public virtual IEnumerable<string> CompanionUrns => Enumerable.Empty<string>();
  }
}
