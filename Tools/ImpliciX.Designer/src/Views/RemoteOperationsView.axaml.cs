using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views;

public partial class RemoteOperationsView : UserControl
{
  public RemoteOperationsView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}