using System;
using System.Diagnostics.CodeAnalysis;

namespace GeBoCommon.Utilities
{
    public class NormalizedPathComparer : StringComparer
    {
        private static readonly ExpiringSimpleCache<string, int> HashCodeCache =
            new ExpiringSimpleCache<string, int>(CalculateHashCode, 180);

        internal NormalizedPathComparer() { }

        
        [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
        public static string NormalizePathString(string input)
        {
            try
            {
                return PathUtils.NormalizePath(input);
            }
            catch
            {
                return input;
            }
        }

        public override bool Equals(string x, string y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            return GetHashCode(x) == GetHashCode(y);
        }

        private static int CalculateHashCode(string obj)
        {
            return OrdinalIgnoreCase.GetHashCode(NormalizePathString(obj));
        }

        public override int GetHashCode(string obj)
        {
            return HashCodeCache[obj];
        }

        public override int Compare(string x, string y)
        {
            return OrdinalIgnoreCase.Compare(NormalizePathString(x), NormalizePathString(y));
        }
    }
}
