using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImpliciX.Language.Core;

namespace ImpliciX.TimeMath.Computers;

public class WindowProcessor<T>
  where T : IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
{

  public WindowProcessor(int windowSize)
  {
    Size = windowSize;
  }

  record Wagon(T All, T Chunk);
  private readonly Queue<Wagon> _window = new();
  public readonly int Size;

  public (T all, Option<T> removed) Push(T entered)
  {
    if (_window.Count == 0)
    {
      _window.Enqueue( new Wagon(entered, entered) );
      return (entered,Option<T>.None());
    }
    var result = ResizeWindow(entered);
    _window.Enqueue( new Wagon(result.newAll, entered) );
    return (result.newAll,result.removed);
  }

  private (T newAll, Option<T> removed) ResizeWindow(T entered)
  {
    var newestWagon = _window.Last();
    var afterAddNew = newestWagon.All + entered;
    if (_window.Count < Size)
      return (
        afterAddNew,
        Option<T>.None()
      );
    var oldestWagon = _window.Dequeue();
    var afterAddNewAndRemoveOldest = afterAddNew - oldestWagon.Chunk;
    return (
      afterAddNewAndRemoveOldest,
      oldestWagon.Chunk
    );
  }
}