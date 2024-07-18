using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text.Json;
using ImpliciX.DesktopServices.Helpers;

namespace ImpliciX.DesktopServices.Services;

internal class History<T> : List<T>
{
  private readonly string _persistenceKey;
  public int Size { get; }
  public readonly Subject<IEnumerable<T>> Subject = new();

  public History(int size, string persistenceKey)
  {
    _persistenceKey = persistenceKey;
    Size = size;
    var history = UserSettings.Read(_persistenceKey);
    if (history != null) AddRange(JsonSerializer.Deserialize<List<T>>(history));
  }

  public void Record(T item)
  {
    if (Count > 0 && this[0].Equals(item))
      return;
    Remove(item);
    Insert(0, item);
    while (Count > Size)
      RemoveAt(Size);
    UserSettings.Set(_persistenceKey, JsonSerializer.Serialize(this, typeof(List<T>)));
    Subject.OnNext(this);
  }
}