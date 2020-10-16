using System;
using GeBoCommon.Chara;
using TranslationHelperPlugin.Translation;

namespace TranslationHelperPlugin.Utils
{
    internal static partial class CharaFileInfoWrapper
    {
        internal static CharacterSex GuessSex(this ICharaFileInfo fileInfo)
        {
            return (CharacterSex)Configuration.GuessSex(fileInfo.Club, fileInfo.Personality);
        }
    }

    internal partial class CharaFileInfoWrapper<T>
    {
        private static readonly Func<T, string> ClubGetter = CreateGetter<string>("club");
        private static readonly Func<T, string> PersonalityGetter = CreateGetter<string>("personality");
        public string Club => ClubGetter(_target);

        public string Personality => PersonalityGetter(_target);
    }
}
