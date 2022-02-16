using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Illusion.Extensions;
using JetBrains.Annotations;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public static class PathUtils
    {
        private static char[] _directorySeparatorsToReplace;

        private static readonly ExpiringSimpleCache<string, string> NormalizedPathCache =
            new ExpiringSimpleCache<string, string>(CalculateNormalizedPath, TimeSpan.FromMinutes(5),
                $"{typeof(PathUtils).PrettyTypeFullName()}.{nameof(NormalizedPathCache)}");

        private static readonly string[] NonNormalizedSubstrings =
        {
            Path.AltDirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar + "..",
            ".." + Path.DirectorySeparatorChar
        };

        private static readonly string[] DirectorySeparators =
        {
            Path.AltDirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar.ToString()
        };


        public static readonly StringComparer NormalizedPathComparer = new NormalizedPathComparer();

        private static ManualLogSource Logger => Common.CurrentLogger;


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
        [PublicAPI]
        public static bool IsNormalized(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            // absolute path check
            return Path.IsPathRooted(path) && !NonNormalizedSubstrings.Any(path.Contains);
        }

        /// <summary>
        ///     Determines whether the specified path is a raw filename (contains no path information).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>
        ///     <c>true</c> if the specified path is a raw filename; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRawFilename(string path)
        {
            // raw filename check
            return !Path.IsPathRooted(path) && !DirectorySeparators.Any(path.Contains);
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
            return NormalizePathSeparators((Path.IsPathRooted(path)
                ? Path.GetFullPath(new Uri(path).LocalPath)
                : path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        /// <summary>
        ///     Normalizes the path separators.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>path with normalized separators</returns>
        [PublicAPI]
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
        [PublicAPI]
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


        private static Predicate<string> GetPathMatchingPredicate(this string path)
        {
            return testPath => NormalizedPathComparer.Equals(path, testPath);
        }

        public static int FindPathIndex(this List<string> pathList, string path)
        {
            return pathList.FindIndex(path.GetPathMatchingPredicate());
        }

        public static string GetTempDirectory(string pluginGuid)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
            var result = string.Empty;

            for (var i = 0; string.IsNullOrEmpty(result) || File.Exists(result) || Directory.Exists(result); i++)
            {
                result = CombinePaths(Paths.CachePath,
                    StringUtils.JoinStrings(".", pluginGuid, timestamp, i.ToString()));
            }

            return result;
        }

        public static string GetTempFile(string tag, string extension, string path = null)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss");
            var result = string.Empty;

            if (path.IsNullOrEmpty()) path = Paths.CachePath;

            for (var i = 0; string.IsNullOrEmpty(result) || File.Exists(result) || Directory.Exists(result); i++)
            {
                result = CombinePaths(path,
                    StringUtils.JoinStrings(".", tag, timestamp, i.ToString(), extension));
            }

            return result;
        }

        public static void ReplaceFile(string sourceFile, string destFile, bool backup = false, bool useCopy = false)
        {
            if (string.IsNullOrEmpty(sourceFile))
            {
                throw new ArgumentException($"{nameof(sourceFile)} must not be null or empty", sourceFile);
            }

            if (!File.Exists(sourceFile)) throw new ArgumentException($"{nameof(sourceFile)} must exist");
            if (string.IsNullOrEmpty(destFile))
            {
                throw new ArgumentException($"{nameof(destFile)} must not be null or empty", destFile);
            }

            var tmpBackup = GetTempFile(Path.GetFileName(destFile), ".bak");
            try
            {
                if (File.Exists(destFile)) File.Move(destFile, tmpBackup);
                if (useCopy)
                {
                    File.Copy(sourceFile, destFile);
                    File.SetAttributes(destFile, File.GetAttributes(sourceFile));
                }
                else
                {
                    File.Move(sourceFile, destFile);
                }

                if (!backup || !File.Exists(tmpBackup)) return;
                var backupFile = StringUtils.JoinStrings('.', destFile, "bak");
                if (File.Exists(backupFile)) File.Delete(backupFile);
                File.Move(tmpBackup, backupFile);
            }
            catch (Exception err)
            {
                Logger.LogError($"Unexpected error replacing file {destFile}: {err.Message}");
                if (File.Exists(tmpBackup))
                {
                    Logger.LogInfo($"Restoring {destFile} backup");
                    File.Move(tmpBackup, destFile);
                }

                throw;
            }
            finally
            {
                if (File.Exists(tmpBackup)) File.Delete(tmpBackup);
            }
        }
    }
}
