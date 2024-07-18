using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views;

public class LivePercentagePropertyView : UserControl
{
  public LivePercentagePropertyView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
