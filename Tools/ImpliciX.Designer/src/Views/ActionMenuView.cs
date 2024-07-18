using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;

namespace ImpliciX.Designer.Views
{
  public class ActionMenuView : IBuild
  {
    public object Build() =>
      new MenuItem
      {
        [!HeaderedSelectingItemsControl.HeaderProperty] = new Binding("Text"),
        [!MenuItem.CommandProperty] = new Binding("Open"),
        [!InputElement.IsEnabledProperty] = new Binding("!IsBusy")
      };
  }
}