using GameLoadCharaFileSystem;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Utils
{
    // GameLoadCharaFileSystem.GameCharaFileInfo
    internal class GameCharaFileInfoWrapper : ICharaFileInfo
    {
        private readonly GameCharaFileInfo _target;

        public GameCharaFileInfoWrapper(GameCharaFileInfo gameCharaFileInfo)
        {
            _target = gameCharaFileInfo;
        }

        public int Index => _target.index;

        public string Name
        {
            get => _target.name;
            set => _target.name = value;
        }

        public string FullPath => _target.FullPath;

        public CharacterSex Sex
        {
            get
            {
                try
                {
                    return (CharacterSex)_target.sex;
                }
                catch
                {
                    return this.GuessSex();
                }
            }
        }
    }
}
