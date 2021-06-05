using System;
using GeBoCommon.Utilities;

namespace TranslationHelperPlugin.Presets
{
    public abstract class CardNameCacheData<T> : CardNameCacheBase, IEquatable<T> where T : CardNameCacheBase
    {
#if KK || KKS
        private const string KeyJoin = "|||||";
#else
        private const string KeyJoin = " ";
#endif

        private readonly SimpleLazy<string> _identity;

        protected CardNameCacheData(string familyName, string givenName) : this(familyName, givenName, null) { }

        protected CardNameCacheData(string fullName) : this(null, null, fullName) { }

        private CardNameCacheData(string familyName, string givenName, string fullName)
        {
            FamilyName = familyName;
            GivenName = givenName;
            FullName = fullName;
            _identity = new SimpleLazy<string>(MakeIdentity);
        }

        public override string FamilyName { get; }

        public override string GivenName { get; }

        public override string FullName { get; }

        internal override string Identity => _identity.Value;

        private string MakeIdentity()
        {
            return (string.IsNullOrEmpty(FullName)
                ? StringUtils.JoinStrings(KeyJoin, FamilyName, GivenName)
                : FullName).Trim();
        }


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((T)obj);
        }

        public bool Equals(T other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || IdentityComparer.Equals(Identity, other.Identity);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
