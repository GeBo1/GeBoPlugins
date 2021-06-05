
using JetBrains.Annotations;
#if KK||KKS
using GeBoCommon.Chara;
#endif
#if AI||HS2
using AIChara;
using TranslationHelperPlugin.Chara;
#endif

namespace TranslationHelperPlugin.Presets
{
    public class CardNameCacheKey : CardNameCacheData<CardNameCacheKey>
    {
        internal CardNameCacheKey(string familyName, string givenName) : base(familyName, givenName) { }

        [UsedImplicitly]
        internal CardNameCacheKey(string fullName) : base(fullName) { }

#if KK || KKS
        internal CardNameCacheKey(ChaFile chaFile) :
            this(chaFile.GetName("lastname"), chaFile.GetName("firstname")) { }
#else
        internal CardNameCacheKey(ChaFile chaFile) : this(chaFile.GetOriginalFullName()) { }
#endif
    }
}
