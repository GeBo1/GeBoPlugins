using ChaCustom;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Utils
{
    internal class CustomFileInfoWrapper : ICharaFileInfo
    {
        private readonly CustomFileInfo _fileInfo;

        public CustomFileInfoWrapper(CustomFileInfo customFileInfo)
        {
            _fileInfo = customFileInfo;
        }

        public int Index => _fileInfo.index;

        public string Name
        {
            get => _fileInfo.name;
            set => _fileInfo.name = value;
        }

        public string FullPath => _fileInfo.FullPath;
        public string Club => _fileInfo.club;
        public string Personality => _fileInfo.personality;

        public CharacterSex Sex => this.GuessSex();
    }
}
