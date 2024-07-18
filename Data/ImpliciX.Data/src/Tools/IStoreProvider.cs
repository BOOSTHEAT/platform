using System;
using System.Threading.Tasks;

namespace ImpliciX.Data.Tools;

public interface IStoreProvider
{
  Task<IStoreFile> TryGetFileFromPathAsync(Uri filePath);
  Task<IStoreFolder> TryGetFolderFromPathAsync(Uri folderPath);
}
