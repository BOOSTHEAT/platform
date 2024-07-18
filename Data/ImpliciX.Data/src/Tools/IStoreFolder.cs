using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImpliciX.Data.Tools;

public interface IStoreFolder : IStoreItem
{
  IAsyncEnumerable<IStoreItem> GetItemsAsync();
  Task<IStoreFile?> CreateFileAsync(string name);
  Task<IStoreFolder?> CreateFolderAsync(string name);

  static async Task<IStoreFile[]> GetFiles(IStoreFolder folder, string fileExtension)
  {
    var extension = fileExtension.StartsWith("*") ? fileExtension.Substring(1) : fileExtension;
    var files = new List<IStoreFile>();
    await foreach (var item in folder.GetItemsAsync())
      if (item is IStoreFile file)
        if (file.Name.EndsWith(extension))
          files.Add(file);
    return files.ToArray();
  }

  static IStoreFolder Combine(IStoreFolder parent, string child)
  {
    return parent.CreateFolderAsync(child).Result;
  }
}
