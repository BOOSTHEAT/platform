using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.SharedKernel.DocTools
{
  public class FSMView : UserControl
  {
    public FSMView()
    {
      this.InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}