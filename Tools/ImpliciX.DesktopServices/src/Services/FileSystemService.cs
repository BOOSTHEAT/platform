using System.IO;

namespace ImpliciX.DesktopServices.Services;

internal sealed class FileSystemService : IFileSystemService
{
  public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
  public bool FileExists(string? path) => File.Exists(path);
  public void DeleteFile(string path) => File.Delete(path);
  public IDirectoryInfoWrapper CreateDirectory(string path) =>
    new DirectoryInfoWrapper(Directory.CreateDirectory(path));

  public string[] DirectoryGetFiles(string path, string searchPattern = null) =>
    searchPattern is null
      ? Directory.GetFiles(path)
      : Directory.GetFiles(path, searchPattern);
}
