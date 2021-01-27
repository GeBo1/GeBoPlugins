using JetBrains.Annotations;

namespace GeBoCommon.AutoTranslation
{
    [PublicAPI]
    public class SimpleComponentTranslationContext : IComponentTranslationContext
    {
        public object Component => null;

        public string OriginalText => null;

        public string OverriddenTranslatedText => null;

        public void IgnoreComponent() { }

        public void OverrideTranslatedText(string translation) { }

        public void ResetBehaviour() { }
    }
}
