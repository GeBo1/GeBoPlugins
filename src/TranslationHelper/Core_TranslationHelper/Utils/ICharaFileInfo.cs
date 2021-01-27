using System.Diagnostics.CodeAnalysis;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Utils
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public partial interface ICharaFileInfo
    {
        int Index { get; }
        string Name { get; set; }
        string FullPath { get; }

        CharacterSex Sex { get; }
       
    }
}
