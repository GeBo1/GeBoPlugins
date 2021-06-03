using System;
using GeBoCommon.Chara;
using JetBrains.Annotations;

namespace TranslationHelperPlugin
{
    public class NameScope : IEquatable<NameScope>
    {
        internal const int BaseScope = 8192;

        public static readonly NameScope DefaultNameScope = new NameScope();

        public NameScope(CharacterSex sex, NameType nameType = NameType.Unclassified)
        {
            NameType = nameType;
            Sex = sex;
            TranslationScope = BaseScope + ((int)NameType * 16) + (int)Sex + 1;
        }

        [PublicAPI]
        public NameScope(NameType nameType) : this(CharacterSex.Unspecified, nameType) { }

        public NameScope() : this(CharacterSex.Unspecified) { }
        public int TranslationScope { get; }
        public NameType NameType { get; }
        public CharacterSex Sex { get; }

        public bool Equals(NameScope other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return TranslationScope == other.TranslationScope;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((NameScope)obj);
        }

        public override int GetHashCode()
        {
            return TranslationScope;
        }

        public override string ToString()
        {
            return $"NameScope({Sex}, {NameType}, {TranslationScope})";
        }
    }
}
