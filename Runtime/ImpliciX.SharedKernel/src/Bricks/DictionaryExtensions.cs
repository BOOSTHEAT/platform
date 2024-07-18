using System;
using System.Collections.Generic;

namespace ImpliciX.SharedKernel.Bricks
{
    public static class DictionaryExtensions
    {
        public static void AddOrUpdate<K, V>(this Dictionary<K, V> self, K key, V defaultValue, Func<V, V> updateFunction)
        {
            self[key] = self.TryGetValue(key, out var value) ? updateFunction(value) : defaultValue;
        }
    }
}