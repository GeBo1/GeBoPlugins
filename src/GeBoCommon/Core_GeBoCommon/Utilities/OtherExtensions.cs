using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;

namespace GeBoCommon.Utilities
{
    public static class OtherExtensions
    {
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> array)
        {
            return array.Select((item, index) => new KeyValuePair<int, T>(index, item));
        }

        public static void DebugLogDebug(this ManualLogSource logger, object obj)
        {
#if DEBUG
            logger.LogDebug(obj);
#endif
        }
    }
}
