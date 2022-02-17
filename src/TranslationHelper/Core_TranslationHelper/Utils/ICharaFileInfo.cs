using System.Diagnostics.CodeAnalysis;
using GeBoCommon.Chara;
using JetBrains.Annotations;

namespace TranslationHelperPlugin.Utils
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public partial interface ICharaFileInfo
    {
        [PublicAPI]
        int Index { get; }

        [PublicAPI]
        string Name { get; set; }

        [PublicAPI]
        string FullPath { get; }

        [PublicAPI]
        CharacterSex Sex { get; }
    }
}
