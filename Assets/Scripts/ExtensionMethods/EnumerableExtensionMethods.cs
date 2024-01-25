using System.Collections.Generic;
using System.Linq;

namespace ExtensionMethods
{
    public static class EnumerableExtensionMethods
    {
        public static IEnumerable<T> InsertBetween<T>(this IEnumerable<T> source, T value, int index)
        {
            var sourceList = source.ToList();
            sourceList.Insert(index, value);
            return sourceList;
        }
    }
}