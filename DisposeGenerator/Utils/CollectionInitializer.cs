using System.Collections.Generic;

namespace DisposeGenerator.Utils
{
    internal static class CollectionInitializer
    {
        public static void Add<T>(this ICollection<T> collection, IEnumerable<T> values)
        {
            if (collection is List<T> list)
            {
                Add(list, values);
                return;
            }

            foreach (var value in values)
                collection.Add(value);
        }

        public static void Add<T>(this List<T> list, IEnumerable<T> values) =>
            list.AddRange(values);
    }
}
