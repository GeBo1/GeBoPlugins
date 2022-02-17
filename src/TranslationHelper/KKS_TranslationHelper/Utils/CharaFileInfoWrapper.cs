using System;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using KKAPI.Maker;
using TranslationHelperPlugin.Translation;

namespace TranslationHelperPlugin.Utils
{
    static partial class CharaFileInfoWrapper
    {
        internal static CharacterSex GuessSex(this ICharaFileInfo fileInfo)
        {
            try
            {
                if (MakerAPI.InsideMaker)
                {
                    return (CharacterSex)MakerAPI.GetMakerSex();
                }
            }
            catch (Exception err)
            {
                Logger?.LogException(err, $"{nameof(GuessSex)}: Unexpected error determining sex from Maker");
            }

            try
            {
                return (CharacterSex)Configuration.GuessSex(fileInfo.Club, fileInfo.Personality);
            }
            catch (Exception err)
            {
                Logger?.LogException(err,
                    $"{nameof(GuessSex)}: Unexpected error attempting to guess sex for {fileInfo.GetPrettyTypeFullName()}");
            }

            return CharacterSex.Unspecified;
        }
    }

    internal partial class CharaFileInfoWrapper<T>
    {
        private static readonly Func<T, string> ClubGetter = CreateGetter<string>("club");
        private static readonly Func<T, string> PersonalityGetter = CreateGetter<string>("personality");

        public string Club => GetterWrapper(ClubGetter, string.Empty);

        public string Personality => GetterWrapper(PersonalityGetter, string.Empty);

        private TOut GetterWrapper<TOut>(Func<T, TOut> getter, TOut fallback)
        {
            try
            {
                return getter(_target);
            }
            catch (Exception err)
            {
                Logger?.LogException(err,
                    $"{this.GetPrettyTypeFullName()}: Unexpected error calling {getter} on {_target.GetPrettyTypeFullName()}");
            }

            return fallback;
        }
    }
}
