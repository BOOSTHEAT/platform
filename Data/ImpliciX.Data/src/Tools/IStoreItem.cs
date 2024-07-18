using System;
using System.Threading.Tasks;

namespace ImpliciX.Data.Tools;

public interface IStoreItem : IComparable<IStoreItem>
{
  string Name { get; }

  Uri Path { get; }

  IStoreFolder Parent => this.GetParentAsync().Result;
  string LocalPath => Path.LocalPath;

  int IComparable<IStoreItem>.CompareTo(IStoreItem other)
  {
    return string.Compare(Path.LocalPath, other.Path.LocalPath, StringComparison.Ordinal);
  }

  Task<IStoreFolder?> GetParentAsync();
}
