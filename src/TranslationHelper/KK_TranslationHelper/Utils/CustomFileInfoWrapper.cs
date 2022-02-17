using ChaCustom;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Utils
{
    internal class CustomFileInfoWrapper : CharaFileInfoWrapper<CustomFileInfo>
    {
        public CustomFileInfoWrapper(CustomFileInfo customFileInfo) : base(customFileInfo) { }

        // used in maker, so get sex that way
        public override CharacterSex Sex => this.GuessSex();
    }
}
