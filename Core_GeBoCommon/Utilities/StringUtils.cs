#if false
using System.Linq;
using System.Text.RegularExpressions;

namespace GeBoCommon.Utilities
{
    public static class StringUtils
    {
        private static readonly Regex ContainsJapaneseCharRegex = new Regex(
            @"([\u3000-\u303F]|[\u3040-\u309F]|[\u30A0-\u30FF]|[\uFF00-\uFFEF]|[\u4E00-\u9FAF]|[\u2605-\u2606]|[\u2190-\u2195]|\u203B)",
            Constants.SupportedRegexCompilationOption);

        public static bool ContainsNonAscii(string input)
        {
            return input.ToCharArray().Any(c => c > sbyte.MaxValue);
        }

        public static bool ContainsJapaneseChar(string input)
        {
            return ContainsJapaneseCharRegex.IsMatch(input);
        }

        /// <summary>Wrapper for <see cref="string.Join(string, string[])" /> to workaround lack of params usage in .NET 3.5.</summary>
        public static string JoinStrings(string separator, params string[] value)
        {
            return string.Join(separator, value);
        }
        public static string JoinStrings(char separator, params string[] value) 
        {
            return string.Join(separator.ToString(), value);
        }
    }
}
#endif
