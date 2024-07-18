using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
  public class SubSystemView : UserControl
  {
    public SubSystemView()
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
