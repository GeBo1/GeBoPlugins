using System.Linq;
using System.Text.RegularExpressions;

namespace GeBoCommon.Utilities
{
    public static class StringUtils
    {
        private static readonly Regex ContainsJapaneseCharRegex = new Regex(
            @"([\u3000-\u303F]|[\u3040-\u309F]|[\u30A0-\u30FF]|[\uFF00-\uFFEF]|[\u4E00-\u9FAF]|[\u2605-\u2606]|[\u2190-\u2195]|\u203B)",
            Constants.SupportedRegexCompilationOption);

        /// <summary>
        /// Determines whether the specified input contains non-ASCII characters.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        ///   <c>true</c> if the specified input contains non-ASCII characters; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsNonAscii(string input)
        {
            return input.ToCharArray().Any(c => c > sbyte.MaxValue);
        }

        /// <summary>
        /// Determines whether the specified input contains Japanese characters.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>
        ///   <c>true</c> if the specified input contains Japanese characters; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsJapaneseChar(string input)
        {
            return ContainsJapaneseCharRegex.IsMatch(input);
        }

        /// <summary>
        /// Wrapper for <see cref="string.Join(string, string[])" /> to workaround lack of params usage in .NET 3.5.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="value">strings to join</param>
        /// <returns>joined string</returns>
        public static string JoinStrings(string separator, params string[] value)
        {
            if (value == null || value.Length == 0) return string.Empty;
            // Concat is faster than string.Join(string.empty, ...) 
            if (string.IsNullOrEmpty(separator)) return string.Concat(value);
            return string.Join(separator, value);
        }


        /// <summary>
        /// Joins the strings.
        /// </summary>
        /// <param name="separator">The separator.</param>
        /// <param name="value">strings to join</param>
        /// <returns>joined string</returns>
        public static string JoinStrings(char separator, params string[] value)
        {
            if (value == null || value.Length == 0) return string.Empty;
            return string.Join(separator.ToString(), value);
        }
    }
}
