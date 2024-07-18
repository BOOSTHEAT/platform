using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices;

public interface IFileSystemService
{
  void WriteAllText([NotNull] string path, [NotNull] string content);
  bool FileExists([CanBeNull] string path);
  void DeleteFile([NotNull] string path);
  IDirectoryInfoWrapper CreateDirectory([NotNull] string path);
  string[] DirectoryGetFiles([NotNull] string path, [CanBeNull] string searchPattern = null);
}

public interface IDirectoryInfoWrapper
{
  string FullName { get; }
  IEnumerable<FileInfo> EnumerateFiles();
}
