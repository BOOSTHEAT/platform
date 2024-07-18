using Avalonia.Controls;
using ImpliciX.Designer.ViewModels;

namespace ImpliciX.Designer.Views;

public partial class WelcomeView : UserControl
{
  public WelcomeView()
  {
    this.InitializeComponent();
  }

  private void SelectingItemsControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (e.AddedItems.Count > 0) ((SessionCommands)e.AddedItems[0])?.Command();
  }
}
