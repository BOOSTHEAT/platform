using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Tests.Doubles
{
    public class SpySubscriber
    {
        private ConcurrentDictionary<Type, List<DomainEvent>> _receivedObjects;

        public int CountOf<T>()
        {
            if (!_receivedObjects.ContainsKey(typeof(T))) return 0;
            return _receivedObjects[typeof(T)].Count;
        }

        public SpySubscriber()
        {
            _receivedObjects = new ConcurrentDictionary<Type, List<DomainEvent>>();
        }

        public void Receive(DomainEvent obj)
        {
            var type = obj.GetType();
            _receivedObjects.AddOrUpdate(type, (_) => new List<DomainEvent>() {obj}, (__, oldList) =>
                {
                    oldList.Add(obj);
                    return oldList;
                }
            );
        }

        public List<T> ReceivedEventsOf<T>()
        {
            return _receivedObjects.TryGetValue(typeof(T), out var events)
                ? events.Cast<T>().ToList()
                : new List<T>();
        }
    }
}