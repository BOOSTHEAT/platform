using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace dialogs_demo;

public partial class App : Application
{
  public override void Initialize()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      desktop.MainWindow = new MainWindow();
    }

    base.OnFrameworkInitializationCompleted();
  }
}