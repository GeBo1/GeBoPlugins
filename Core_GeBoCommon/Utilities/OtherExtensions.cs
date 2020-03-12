using System.Collections.Generic;
using System.Linq;

namespace GeBoCommon.Utilities
{
    public static class OtherExtensions
    {
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> array)
        {
            return array.Select((item, index) => new KeyValuePair<int, T>(index, item));
        }
    }
}