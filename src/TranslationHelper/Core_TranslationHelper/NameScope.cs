using System.Diagnostics.CodeAnalysis;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin
{
    public class NameScope
    {
        internal const int BaseScope = 8192;

        public NameScope(CharacterSex sex, NameType nameType)
        {
            NameType = nameType;
            Sex = sex;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public NameScope(NameType nameType) : this(CharacterSex.Unspecified, nameType) { }

        public NameScope(CharacterSex sex) : this(sex, NameType.Unclassified) { }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public NameScope() : this(CharacterSex.Unspecified, NameType.Unclassified) { }

        public int TranslationScope => BaseScope + ((int)NameType * 16) + (int)Sex + 1;
        public NameType NameType { get; }
        public CharacterSex Sex { get; }

        public override string ToString()
        {
            return $"NameScope({Sex},{NameType},{TranslationScope})";
        }
    }
}
