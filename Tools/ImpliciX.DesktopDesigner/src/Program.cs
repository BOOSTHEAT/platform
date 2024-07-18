using System;
using Avalonia;
using Avalonia.ReactiveUI;

namespace ImpliciX.DesktopServices;

internal sealed class Program
{
  // Initialization code. Don't use any Avalonia, third-party APIs or any
  // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
  // yet and stuff might break.
  [STAThread]
  public static void Main(string[] args)
  {
    BuildAvaloniaApp()
      .StartWithClassicDesktopLifetime(args);
  }

  // Avalonia configuration, don't remove; also used by visual designer.
  public static AppBuilder BuildAvaloniaApp()
    => AppBuilder
#if MONITOR
      .Configure<Designer.Monitor>()
#else
      .Configure<Designer.Designer>()
#endif
      .UsePlatformDetect()
      .WithInterFont()
      .LogToTrace()
      .UseReactiveUI();
}
