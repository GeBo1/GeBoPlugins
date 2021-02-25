using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Illusion.Extensions;
using JetBrains.Annotations;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public static class PathUtils
    {
        private static char[] _directorySeparatorsToReplace;

        private static readonly ExpiringSimpleCache<string, string> NormalizedPathCache =
            new ExpiringSimpleCache<string, string>(CalculateNormalizedPath, 180);

        private static readonly string[] NonNormalizedSubstrings =
        {
            Path.AltDirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar + "..",
            ".." + Path.DirectorySeparatorChar
        };


        public static readonly StringComparer NormalizedPathComparer = new NormalizedPathComparer();


        private static IEnumerable<char> DirectorySeparatorsToReplace
        {
            get
            {
                if (_directorySeparatorsToReplace != null) return _directorySeparatorsToReplace;

                var dirSeparators = HashSetPool<char>.Get();
                 try
                {
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
                finally
                {
                    HashSetPool<char>.Release(dirSeparators);
                }
            }
        }

        /// <summary>
        ///     Determines whether the specified path is normalized (contains only standard path separators and doesn't contain
        ///     relative paths)
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///     <c>true</c> if the specified path is normalized; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNormalized(string path)
        {
            return path != null && path[1] == ':' && !NonNormalizedSubstrings.Any(path.Contains);
        }

        /// <summary>
        ///     Normalizes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>normalized path</returns>
        public static string NormalizePath(string path)
        {
            return NormalizedPathCache.Get(path);
        }

        private static string CalculateNormalizedPath(string path)
        {
            if (string.IsNullOrEmpty(path) || IsNormalized(path)) return path;
            return NormalizePathSeparators(Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        /// <summary>
        ///     Normalizes the path separators.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>path with normalized separators</returns>
        public static string NormalizePathSeparators(string path)
        {
            // avoid Path.Combine, blows up on '..' in the middle somewhere
            var parts = path?.Split((char[])DirectorySeparatorsToReplace);
            return parts == null || parts.Length <= 1
                ? path
                : StringUtils.JoinStrings(Path.DirectorySeparatorChar, parts);
        }

        /// <summary>
        ///     Combines path segments with normalized path separators.
        /// </summary>
        /// <param name="parts">The parts.</param>
        /// <returns>combined path</returns>
        public static string CombinePaths(params string[] parts)
        {
            var splitChars = (char[])DirectorySeparatorsToReplace;
            return parts == null || parts.Length == 0
                ? null
                : StringUtils.JoinStrings(Path.DirectorySeparatorChar,
                    parts.SelectMany(i => i.Split(splitChars)).ToArray());
        }

        /// <summary>
        ///     Splits the path on path separators
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>path sections</returns>
        public static string[] SplitPath(string path)
        {
            return path?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

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
