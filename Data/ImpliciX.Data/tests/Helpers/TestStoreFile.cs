using System.IO;
using System.Threading.Tasks;
using ImpliciX.Data.Tools;

namespace ImpliciX.Data.Tests.Helpers;

#pragma warning disable CS1998
public class TestStoreFile : TestStoreItem, IStoreFile
{
  public TestStoreFile(string fileName) : this(new FileInfo(fileName))
  {
  }

  private TestStoreFile(FileInfo file) : base(file)
  {
  }

  private FileInfo FileInfo => new(FullName);

  public async Task<Stream> OpenReadAsync() =>
    FileInfo.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);

  public async Task<Stream> OpenWriteAsync() =>
    FileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

  protected override DirectoryInfo GetParent() => FileInfo.Directory;
}
