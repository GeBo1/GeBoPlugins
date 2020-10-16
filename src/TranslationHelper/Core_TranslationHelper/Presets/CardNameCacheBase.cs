using System;
using System.Collections.Generic;
using GeBoCommon.Utilities;

namespace TranslationHelperPlugin.Presets
{
    internal interface ICardNameCacheBase
    {
        string FamilyName { get; }
        string GivenName { get; }
        string FullName { get; }
    }

    public abstract class CardNameCacheBase : ICardNameCacheBase, IEquatable<CardNameCacheBase>
    {
        protected static readonly IEqualityComparer<string> IdentityComparer = new TrimmedStringComparer();
        internal abstract string Identity { get; }

        public abstract string FamilyName { get; }

        public abstract string GivenName { get; }

        public abstract string FullName { get; }

        public bool Equals(CardNameCacheBase other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || IdentityComparer.Equals(Identity, other.Identity);
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return ReferenceEquals(this, obj) || (
                obj.GetType() == GetType() && Equals((CardNameCacheBase)obj));
        }

        public override int GetHashCode()
        {
            return Identity != null ? Identity.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return $"{GetType().Name}({Identity})";
        }
    }
}
