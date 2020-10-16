namespace TranslationHelperPlugin.Presets
{
    internal class CardNameCacheValue : CardNameCacheData<CardNameCacheValue>
    {
        internal CardNameCacheValue(string familyName, string givenName) : base(familyName, givenName) { }

        // ReSharper disable once UnusedMember.Global
        internal CardNameCacheValue(string fullName) : base(fullName) { }
    }
}
