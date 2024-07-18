using System;
using System.IO;
using System.Threading.Tasks;
using ImpliciX.Data.Tools;

namespace ImpliciX.Data.Tests.Helpers;

public abstract class TestStoreItem : IStoreItem
{
  protected TestStoreItem(FileSystemInfo fileSystemInfo)
  {
    Name = fileSystemInfo.Name;
    FullName = fileSystemInfo.FullName;
    var builder = new UriBuilder("file://");
    builder.Path = FullName;
    Path = builder.Uri;
  }

  protected string FullName { get; }

  public string Name { get; }
  public Uri Path { get; }

  public async Task<IStoreFolder> GetParentAsync() => new TestStoreFolder(GetParent());

  protected abstract DirectoryInfo GetParent();
}
