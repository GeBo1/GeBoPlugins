using Illusion.Extensions;
using System;
using System.IO;
using System.Linq;

namespace GeBoCommon.Utilities
{
    public static class PathUtils
    {
        private static readonly string[] NonNormalizedSubstrings =
        {
            Path.AltDirectorySeparatorChar.ToString(),
            Path.DirectorySeparatorChar.ToString() + ".."
        };

        public static bool IsNormalized(string path)
        {
            return path[1] == ':' && !NonNormalizedSubstrings.Any(s => path.Contains(s));
        }

        public static string NormalizePath(string path)
        {
            if (IsNormalized(path))
            {
                return path;
            }
            return Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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

            Uri rootUri = new Uri(relativeTo + Path.DirectorySeparatorChar, UriKind.Absolute);
            Uri pathUri = new Uri(path, UriKind.Absolute);

            Uri relativeUri = rootUri.MakeRelativeUri(pathUri);

            return Uri.UnescapeDataString(relativeUri.ToString());
        }
    }
}
