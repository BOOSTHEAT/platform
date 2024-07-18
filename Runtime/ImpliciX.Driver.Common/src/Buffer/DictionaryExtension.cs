using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;

namespace ImpliciX.Driver.Common.Buffer
{
    internal static class DictionaryExtension
    {
        internal static Result<TV> GetOrDefault<TK, TV>(this Dictionary<TK, TV> @this, TK k, TV defaultValue)
        {
            return @this.Get(k).Match(_ => Result<TV>.Create(defaultValue), Result<TV>.Create);
        }

        internal static Result<V> Get<K, V>(this Dictionary<K, V> @this, K k)
        {
            try
            {
                return @this[k];
            }
            catch (Exception e)
            {
                return Result<V>.Create(new Error(nameof(DictionaryExtension), e.Message));
            }
        }
    }
}