using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaUI.PrintToPDF;
using ImpliciX.Designer.Features;
using ImpliciX.Designer.ViewModels;
using ImpliciX.Designer.Views;

namespace ImpliciX.Designer;

public abstract class App : Application
{
  public override void Initialize()
  {
    var cultureInfo = CultureInfo.InvariantCulture;
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
    Thread.CurrentThread.CurrentCulture = cultureInfo;
    Thread.CurrentThread.CurrentUICulture = cultureInfo;
    AvaloniaXamlLoader.Load(this);
  }

  public override void OnFrameworkInitializationCompleted()
  {
    var features = this.CreateFeatures();

    var mwvm = new MainWindowViewModel(features);
    TopLevel topLevel = null;
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
      var mainWindow = new MainWindow
      {
        DataContext = mwvm
      };
      desktop.MainWindow = mainWindow;
      topLevel = mainWindow;
    }
    else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
    {
      var mainView = new MainView
      {
        DataContext = mwvm
      };
      singleViewPlatform.MainView = mainView;
      topLevel = TopLevel.GetTopLevel(mainView);
    }

    InitializeWindows(topLevel, features);
    mwvm.SaveAsPdf = path => topLevel.FindAllVisuals<SystemView>().First().SaveAsPdf(path);

    base.OnFrameworkInitializationCompleted();
  }

  protected abstract IFeatures CreateFeatures();

  private static void InitializeWindows(TopLevel topLevel, IFeatures features
  )
  {
    features.RegisterUserOn(topLevel);
    CommandLine.Process(features);
  }
}

public class Designer : App
{
  protected override IFeatures CreateFeatures()
  {
    return new DesignerFeatures();
  }
}

public class Monitor : App
{
  protected override IFeatures CreateFeatures()
  {
    return new MonitorFeatures();
  }
}
