#if !KK
#define HAS_ARRAY_EMPTY
#endif
using JetBrains.Annotations;
#if HAS_ARRAY_EMPTY
using System;
#endif

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public static class ObjectUtils
    {
        [PublicAPI]
        public static int GetCombinedHashCode(params object[] objects)
        {
            var result = 17;
            foreach (var obj in objects)
            {
                unchecked
                {
                    result = (result * 31) + obj.GetHashCode();
                }
            }

            return result;
        }

        [PublicAPI]
        public static T[] GetEmptyArray<T>()
        {
#if HAS_ARRAY_EMPTY
            return Array.Empty<T>();
#else
            return new T[0];
#endif
        }
    }
}
