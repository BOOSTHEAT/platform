using System.Threading.Tasks;

namespace ImpliciX.DesktopServices;

public interface IOperatingSystem
{
  Task OpenUrl(string url);
  Task OpenTerminal(string command);
}