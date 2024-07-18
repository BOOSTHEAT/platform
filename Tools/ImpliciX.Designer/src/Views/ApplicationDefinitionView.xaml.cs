using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
  public class ApplicationDefinitionView : UserControl
  {
    public ApplicationDefinitionView()
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
