using System;

namespace ImpliciX.DesktopServices.Services;

internal class ConsoleService : IConsoleService
{
  public event EventHandler<string> LineWritten;

  public void WriteLine(string text)
  {
    this.LineWritten?.Invoke(this, text);
  }

  public event EventHandler<Exception> Errors;

  public void WriteError(Exception e)
  {
    this.Errors?.Invoke(this, e);
  }
}
