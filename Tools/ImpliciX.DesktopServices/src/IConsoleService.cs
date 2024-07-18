using System;

namespace ImpliciX.DesktopServices;

public interface IConsoleService
{
  event EventHandler<string> LineWritten;
  void WriteLine(string text);
  event EventHandler<Exception> Errors;
  void WriteError(Exception e);
}