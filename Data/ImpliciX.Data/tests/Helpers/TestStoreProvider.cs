using System;
using System.Threading.Tasks;
using ImpliciX.Data.Tools;

namespace ImpliciX.Data.Tests.Helpers;

public class TestStoreProvider : IStoreProvider
{
  public async Task<IStoreFile> TryGetFileFromPathAsync(Uri filePath) => new TestStoreFile(filePath.LocalPath);

  public async Task<IStoreFolder> TryGetFolderFromPathAsync(Uri folderPath) =>
    new TestStoreFolder(folderPath.LocalPath);
}
