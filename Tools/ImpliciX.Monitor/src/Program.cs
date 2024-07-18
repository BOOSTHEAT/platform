using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.ReactiveUI;

[assembly: SupportedOSPlatform("browser")]

namespace ImpliciX.WebMonitor;

public sealed class Program
{
  private static async Task Main(string[] args)
  {
    // var builder = WebAssemblyHostBuilder.CreateDefault(args);
    var options = new BrowserPlatformOptions();
    // JSHost.ImportAsync("FileSystemStorageAccessor", "./js/FileSystemStorageAccessor.js");
    // var FileSystemStorageAccessor = new FileSystemStorageAccessor();

    // builder.Services.AddScoped<LocalStorageAccessor>();
    await BuildAvaloniaApp()
      .WithInterFont()
      .UseReactiveUI()
      .StartBrowserAppAsync("out", options);
  }

  public static AppBuilder BuildAvaloniaApp()
  {
    return AppBuilder
        .Configure<Designer.Monitor>()
      ;
  }
}
