using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImpliciX.Data.Tools;

namespace ImpliciX.Data.Tests.Helpers;

public class TestStoreFolder : TestStoreItem, IStoreFolder
{
  public TestStoreFolder(string folderName) : this(new DirectoryInfo(folderName))
  {
  }

  internal TestStoreFolder(DirectoryInfo folder) : base(folder)
  {
  }

  private DirectoryInfo DirectoryInfo => new(FullName);

  public async IAsyncEnumerable<IStoreItem> GetItemsAsync()
  {
    var fileInfos = DirectoryInfo.GetFiles();
    foreach (var info in fileInfos) yield return new TestStoreFile(info.FullName);
    var directoryInfos = DirectoryInfo.GetDirectories();
    foreach (var info in directoryInfos) yield return new TestStoreFolder(info.FullName);
  }

  public async Task<IStoreFile> CreateFileAsync(string name)
  {
    var filePath = System.IO.Path.Combine(FullName, name);
    return new TestStoreFile(filePath);
  }

  public async Task<IStoreFolder> CreateFolderAsync(string name)
  {
    var prefix = "";
    if (name.StartsWith("/")) prefix = ".";
    var d = DirectoryInfo.CreateSubdirectory(prefix + name);
    return new TestStoreFolder(d);
  }

  protected override DirectoryInfo GetParent() => DirectoryInfo.Parent;

  public static IStoreFolder getTempFolder()
  {
    var p = System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetTempFileName());
    Directory.CreateDirectory(p);
    return new TestStoreFolder(p);
  }
}
