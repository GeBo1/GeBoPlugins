using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public static class EnumerableUtils
    {
        /// <summary>
        ///     Checks if one enumerable can be found within another
        /// </summary>
        /// <param name="haystack">Searching inside this.</param>
        /// <param name="needle">Searching for this.</param>
        /// <param name="comparer">IComparer to use</param>
        /// <returns>true if found, otherwise false</returns>
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
            // while first item exists in remaining haystack
            while ((start = haystackList.IndexOf(needleList[0], start)) != -1)
            {
                // only keep checking if haystack would fit
                if (start + needleLength > haystackLength) break;

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
