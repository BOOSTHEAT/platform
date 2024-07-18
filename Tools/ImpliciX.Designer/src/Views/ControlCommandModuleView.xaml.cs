using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
  public class ControlCommandModuleView : UserControl
  {
    public ControlCommandModuleView()
    {
      this.InitializeComponent();
      this.DisplayOn("screen");
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
