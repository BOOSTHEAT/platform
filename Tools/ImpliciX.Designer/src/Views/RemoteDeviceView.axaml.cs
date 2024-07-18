using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views;

public partial class RemoteDeviceView : UserControl
{
  public RemoteDeviceView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}