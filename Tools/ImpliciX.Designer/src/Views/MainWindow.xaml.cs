using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ImpliciX.Designer.ViewModels;
using JetBrains.Annotations;

namespace ImpliciX.Designer.Views;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
    Opened += async (sender, args) =>
    {
      if (MainWindowViewModel != null)
        await MainWindowViewModel.OnOpened();
    };
#if DEBUG
    this.AttachDevTools();
#endif
  }

  [CanBeNull] private MainWindowViewModel MainWindowViewModel => DataContext as MainWindowViewModel;

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }
}
