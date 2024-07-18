using System;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services.Project;

internal interface IProjectHelper
{
  string CreateTempDirectory();
  string FindGitDirectory(string filePath, int max_depth = 4);
  void CopyAndPrepareProjectDirectory(string sourceFolder, string destFolder);
  Task Until(Func<bool> condition, int timeout = 10_000);
}