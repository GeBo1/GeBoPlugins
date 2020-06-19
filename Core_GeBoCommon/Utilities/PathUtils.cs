using Illusion.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GeBoCommon.Utilities
{
    public static class PathUtils
    {
        private static char[] _directorySeparatorsToReplace;

        private static IEnumerable<char> DirectorySeparatorsToReplace
        {
            get
            {
                if (_directorySeparatorsToReplace != null) return _directorySeparatorsToReplace;

                var dirSeparators = new HashSet<char>();
                if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
                {
                    dirSeparators.Add(Path.AltDirectorySeparatorChar);
                }

                switch (Path.DirectorySeparatorChar)
                {
                    case '\\':
                        dirSeparators.Add('/');
                        break;
                    case '/':
                        dirSeparators.Add('\\');
                        break;
                }

                return _directorySeparatorsToReplace = dirSeparators.ToArray();
            }
        }

        private static readonly string[] NonNormalizedSubstrings =
        {
            Path.AltDirectorySeparatorChar.ToString(),
            Path.DirectorySeparatorChar + "..",
            ".." + Path.DirectorySeparatorChar
        };

        public static bool IsNormalized(string path)
        {
            return path != null && path[1] == ':' && !NonNormalizedSubstrings.Any(path.Contains);
        }

        public static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) || IsNormalized(path)
                ? path
                : NormalizePathSeparators(Path.GetFullPath(Path.GetFullPath(new Uri(path).LocalPath))
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        public static string NormalizePathSeparators(string path)
        {
            // avoid Path.Combine, blows up on '..' in the middle somewhere
            var parts = path?.Split((char[])DirectorySeparatorsToReplace);
            return parts == null || parts.Length <= 1
                ? path
                : StringUtils.JoinStrings(Path.DirectorySeparatorChar, parts);
        }

        public static string CombinePaths(params string[] parts)
        {
            var splitChars = (char[])DirectorySeparatorsToReplace;
            return parts == null || parts.Length == 0
                ? null
                : StringUtils.JoinStrings(Path.DirectorySeparatorChar,
                        parts.SelectMany(i => i.Split(splitChars)).ToArray());
        }

        public static string[] SplitPath(string path) =>
            path?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        public static string GetRelativePath(string relativeTo, string path)
        {
            if (relativeTo is null)
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            relativeTo = NormalizePath(relativeTo.ToLowerInvariant());
            path = NormalizePath(path.ToLowerInvariant());

            if (relativeTo.Compare(path, StringComparison.InvariantCultureIgnoreCase))
            {
                return ".";
            }

            var rootUri = new Uri(relativeTo + Path.DirectorySeparatorChar, UriKind.Absolute);
            var pathUri = new Uri(path, UriKind.Absolute);

            var relativeUri = rootUri.MakeRelativeUri(pathUri);

            return Uri.UnescapeDataString(relativeUri.ToString());
        }
    }
}
