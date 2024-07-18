using System;
using System.IO;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Services;

namespace ImpliciX.DesktopServices;

public interface IConsoleOutputSlice : IDisposable
{
  Task DumpInto(Stream stream);
  Task DumpInto(string filePath);
  static IConsoleOutputSlice Create(IConsoleService console) => new ConsoleOutputSlice(console);
}
