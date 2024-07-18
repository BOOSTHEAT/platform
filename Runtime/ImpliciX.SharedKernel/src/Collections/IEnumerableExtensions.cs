using System;
using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.SharedKernel.Collections
{
    public static class IEnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var element in enumerable)
                action(element);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable) => !enumerable.Any();
    }
}