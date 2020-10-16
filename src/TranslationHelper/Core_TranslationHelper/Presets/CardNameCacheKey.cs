#if KK
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

        // ReSharper disable once UnusedMember.Global
        internal CardNameCacheKey(string fullName) : base(fullName) { }

#if KK
        internal CardNameCacheKey(ChaFile chaFile) :
            this(chaFile.GetName("lastname"), chaFile.GetName("firstname")) { }
#else
        internal CardNameCacheKey(ChaFile chaFile) : this(chaFile.GetOriginalFullName()) { }
#endif
    }
}
