using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
  public class LiveTextPropertyView : UserControl
  {
    public LiveTextPropertyView()
    {
      this.InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
