using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;

namespace ImpliciX.Designer.Views
{
  public class LiveConnectView : UserControl
  {
    public LiveConnectView()
    {
      this.InitializeComponent();
      _defaultButton = this.Find<Button>("Connect")!;
    }

    [NotNull] private readonly Button _defaultButton;

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }

    private void InputElement_OnKeyUp(object sender, KeyEventArgs e)
    {
      if(e.Key == Key.Return)
        _defaultButton.Command?.Execute(null);
    }
  }
}
