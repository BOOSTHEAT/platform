using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
  public class MainWindowDockView : UserControl
  {
    public MainWindowDockView()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}