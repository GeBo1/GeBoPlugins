using System;
using System.IO;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;

namespace TranslationHelperPlugin.Utils
{
    internal static partial class CharaFileInfoWrapper
    {
        private static readonly string
            MalePathKey = PathUtils.CombinePaths(string.Empty, "chara", "male", string.Empty).ToLowerInvariant();

        private static readonly string
            FemalePathKey = PathUtils.CombinePaths(string.Empty, "chara", "female", string.Empty).ToLowerInvariant();

        internal static CharacterSex GuessSex(this ICharaFileInfo fileInfo)
        {
            var filename = Path.GetFileName(fileInfo.FullPath);

            if (filename.StartsWith("AISChaM_", StringComparison.OrdinalIgnoreCase) ||
                filename.StartsWith("ais_m_", StringComparison.OrdinalIgnoreCase) ||
                filename.StartsWith("HS2ChaM_", StringComparison.OrdinalIgnoreCase) ||
                filename.StartsWith("HS2_ill_M_", StringComparison.OrdinalIgnoreCase))
            {
                return CharacterSex.Male;
            }

            if (filename.StartsWith("AISChaF", StringComparison.OrdinalIgnoreCase) ||
                filename.StartsWith("ais_f_", StringComparison.OrdinalIgnoreCase) ||
                filename.StartsWith("HS2ChaF_", StringComparison.OrdinalIgnoreCase) ||
                filename.StartsWith("HS2_ill_F_", StringComparison.OrdinalIgnoreCase))
            {
                return CharacterSex.Female;
            }

            var path = PathUtils.NormalizePath(fileInfo.FullPath).ToLowerInvariant();
            Logger.LogDebug($"{nameof(GuessSex)}: {fileInfo}: {path}");
            return path.Contains(MalePathKey)
                ? CharacterSex.Male
                : path.Contains(FemalePathKey)
                    ? CharacterSex.Female
                    : CharacterSex.Unspecified;
        }
    }
}
