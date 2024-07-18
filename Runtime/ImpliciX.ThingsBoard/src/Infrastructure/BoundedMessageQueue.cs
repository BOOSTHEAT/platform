using System.Collections.Generic;

namespace ImpliciX.ThingsBoard.Infrastructure
{
  public class BoundedQueue<T> : Queue<T>
  {
    private readonly uint _maxCapacity;

    public BoundedQueue(uint maxCapacity)
    {
      _maxCapacity = maxCapacity;
    }

    public new void Enqueue(T element)
    {
      if (Count >= _maxCapacity)
        Dequeue();
      base.Enqueue(element);
    }
  }
}