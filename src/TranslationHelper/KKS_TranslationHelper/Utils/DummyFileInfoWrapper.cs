using GeBoCommon.Chara;

namespace TranslationHelperPlugin.Utils
{
    internal class DummyFileInfoWrapper : ICharaFileInfo
    {
        internal DummyFileInfoWrapper(int index, string name, string club, string personality, string fullPath)
        {
            Index = index;
            Name = name;
            FullPath = fullPath;
            Club = club;
            Personality = personality;
        }

        public int Index { get; }
        public string Name { get; set; }
        public string FullPath { get; }
        public string Club { get; }
        public string Personality { get; }
        public CharacterSex Sex => this.GuessSex();
    }
}
