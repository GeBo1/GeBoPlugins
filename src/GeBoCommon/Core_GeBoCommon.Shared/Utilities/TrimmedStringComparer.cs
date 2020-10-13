using System;
using System.Collections.Generic;
using System.Linq;

namespace GeBoCommon.Utilities
{
    /// <summary>
    ///     Represents a string comparison operation that pre-processes the strings by trimming them.
    /// </summary>
    public class TrimmedStringComparer : StringComparer
    {
        private readonly StringComparer _baseStringComparer;
        private readonly char[] _extraTrimChars;

        private TrimmedStringComparer(char[] extraTrimChars, StringComparer stringComparer)
        {
            _extraTrimChars = (extraTrimChars == null || extraTrimChars.Length == 0) ? null : extraTrimChars;
            _baseStringComparer = stringComparer ?? CurrentCulture;
        }

        public TrimmedStringComparer(StringComparer stringComparer) : this(null, stringComparer) { }
        public TrimmedStringComparer(params char[] extraTrimChars) : this(extraTrimChars, null) { }

        public TrimmedStringComparer() : this(new char[0]) { }

        public TrimmedStringComparer(IEnumerable<char> extraTrimChars, StringComparer stringComparer = null) :
            this(extraTrimChars.ToArray(), stringComparer) { }


        private string TrimString(string input)
        {
            var result = input?.Trim();
            if (string.IsNullOrEmpty(result)) return result;
            if (_extraTrimChars != null) result = result.Trim(_extraTrimChars).Trim();
            return result;
        }

        public override bool Equals(string x, string y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;

            return _baseStringComparer.Equals(TrimString(x), TrimString(y));
        }


        public override int GetHashCode(string obj)
        {
            return _baseStringComparer.GetHashCode(TrimString(obj));
        }

        public override int Compare(string x, string y)
        {
            return _baseStringComparer.Compare(TrimString(x), TrimString(y));
        }
    }
}
