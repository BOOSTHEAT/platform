using Avalonia.Collections;
using Avalonia.Threading;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels
{
  public class ConsoleViewModel : DockableViewModel
  {
    public ConsoleViewModel(ILightConcierge concierge)
    {
      Title = "Console";
      concierge.Console.LineWritten += (sender, text) =>
      {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
          Rows.Add(text);
          while(Rows.Count > MaxRows)
            Rows.RemoveAt(0);
        });
      };
      concierge.Console.Errors += (sender, e) =>
      {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
          await new Errors(concierge).Display(e);
        });
      };
    }

    private const int MaxRows = 500;
    public AvaloniaList<string> Rows { get; } = new AvaloniaList<string>();
  }
}