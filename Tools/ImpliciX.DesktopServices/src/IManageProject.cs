using System.IO;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices;

public interface IManageProject
{
  string Path { get; }
  Task<IDeviceDefinition> Make();
  bool CanMakeMultipleTimes { get; }
  Task<FileInfo> CreatePackage(SystemInfo systemInfo);
  Task RunGui();
}