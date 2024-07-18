using System;
using System.Collections.Generic;
using System.IO;

namespace ImpliciX.DesktopServices.Services;

internal sealed class DirectoryInfoWrapper : IDirectoryInfoWrapper
{
  private readonly DirectoryInfo _directoryInfo;

  public DirectoryInfoWrapper(DirectoryInfo directoryInfo)
  {
    _directoryInfo = directoryInfo ?? throw new ArgumentNullException(nameof(directoryInfo));
  }

  public string FullName => _directoryInfo.FullName;
  public IEnumerable<FileInfo> EnumerateFiles() => _directoryInfo.EnumerateFiles();
}
