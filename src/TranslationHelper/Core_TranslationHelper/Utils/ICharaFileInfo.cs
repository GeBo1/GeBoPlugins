using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Utils
{
    // ReSharper disable once PartialTypeWithSinglePart
    public partial interface ICharaFileInfo
    {
        int Index { get; }
        string Name { get; set; }
        string FullPath { get; }

        CharacterSex Sex { get; }
       
    }
}
