using JetBrains.Annotations;

namespace TranslationHelperPlugin.Presets
{
    internal class CardNameCacheValue : CardNameCacheData<CardNameCacheValue>
    {
        internal CardNameCacheValue(string familyName, string givenName) : base(familyName, givenName) { }

        [UsedImplicitly]
        internal CardNameCacheValue(string fullName) : base(fullName) { }
    }
}
