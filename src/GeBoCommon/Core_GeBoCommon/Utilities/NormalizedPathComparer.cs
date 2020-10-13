using System;

namespace GeBoCommon.Utilities
{
    public class NormalizedPathComparer : StringComparer
    {
        internal NormalizedPathComparer() : base() { }


        private static string NormalizePathString(string input)
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
            // try to compare without normalizing first in case they already match
            return OrdinalIgnoreCase.Equals(x, y) || OrdinalIgnoreCase.Equals(NormalizePathString(x), NormalizePathString(y));
        }


        public override int GetHashCode(string obj)
        {
            return OrdinalIgnoreCase.GetHashCode(NormalizePathString(obj));
        }

        public override int Compare(string x, string y)
        {
            return OrdinalIgnoreCase.Compare(NormalizePathString(x), NormalizePathString(y));
        }
    }
}
