using System;
using System.Collections.Generic;

namespace ImpliciX.SharedKernel.Tools
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> @this,TKey key)
        {
            return @this.ContainsKey(key) ? @this[key] : default(TValue);
        }

        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> @this, TKey key, Func<TKey, TValue> valueFactory)
        {
            if (!@this.ContainsKey(key))
            {
                @this[key] = valueFactory(key);
            }
            return @this[key];
        }
    }
}
