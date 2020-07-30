using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GeBoCommon.Utilities
{
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Utility class")]
    public static class EnumerableUtils
    {
        public static bool EnumerableContains<T>(IEnumerable<T> haystack, IEnumerable<T> needle,
            IComparer<T> comparer = null) where T : IComparable
        {
            if (haystack is null) return false;
            var haystackList = haystack.ToList();
            var haystackLength = haystackList.Count;
            var needleList = needle.ToList();
            var needleLength = needleList.Count;

            comparer = comparer ?? Comparer<T>.Default;

            var start = 0;
            // while first character exists in remaining haystack
            while ((start = haystackList.IndexOf(needleList[0], start)) != -1)
            {
                if (start + needleLength > haystackLength)
                {
                    // can't fit in remaining bytes
                    break;
                }

                var found = true;
                for (var i = 1; i < needleLength; i++)
                {
                    if (comparer.Compare(needleList[i], haystackList[start + i]) == 0) continue;

                    // mismatch
                    found = false;
                    break;
                }

                if (found) return true;
            }

            return false;
        }
    }
}
