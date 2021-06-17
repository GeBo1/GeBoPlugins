using ChaCustom;

namespace TranslationHelperPlugin.Utils
{
    internal class CustomFileInfoWrapper : CharaFileInfoWrapper<CustomFileInfo>, ICharaFileInfo
    {
        public CustomFileInfoWrapper(CustomFileInfo customFileInfo) : base(customFileInfo) { }
    }
}
