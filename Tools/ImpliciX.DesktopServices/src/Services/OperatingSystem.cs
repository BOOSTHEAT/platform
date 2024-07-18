using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services;

internal sealed class OperatingSystem : IOperatingSystem
{
  public Task OpenUrl(string url)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      using Process process = new Process
      {
        StartInfo =
        {
          UseShellExecute = true,
          FileName = url
        }
      };
      process.Start();
      return Task.CompletedTask;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      Process.Start("xdg-open", url);
      return Task.CompletedTask;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      Process.Start("open", url);
      return Task.CompletedTask;
    }
    return Task.CompletedTask;
  }

  public Task OpenTerminal(string command)
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      Process.Start("C:\\Windows\\System32\\cmd.exe", $"/C {command}");
      return Task.CompletedTask;
    }
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      var terminalEmulator =
        (Environment.GetEnvironmentVariable("BH_TERMINAL_EMULATOR") ?? "gnome-terminal --")
        .Split(' ', StringSplitOptions.RemoveEmptyEntries);
      var arguments = string.Join(' ', terminalEmulator.Skip(1)) + " " + command;
      Process.Start(terminalEmulator[0], arguments);
      return Task.CompletedTask;
    }
    return Task.CompletedTask;
  }
}