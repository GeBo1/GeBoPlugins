using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Text;
using GeBoCommon.Chara;

namespace TranslationHelperPlugin
{
    public class NameScope
    {
        internal const int BaseScope = 8192;
        public int TranslationScope => BaseScope + ((int) NameType * 16) + (int) Sex + 1;
        public NameType NameType { get; }
        public CharacterSex Sex { get; }

        public NameScope(CharacterSex sex, NameType nameType)
        {
            NameType = nameType;
            Sex = sex;
        }

        public NameScope(NameType nameType) : this(CharacterSex.Unspecified, nameType) { }
        public NameScope(CharacterSex sex) : this(sex, NameType.Unclassified) { }
        public NameScope() : this(CharacterSex.Unspecified, NameType.Unclassified) { }



    }
}
