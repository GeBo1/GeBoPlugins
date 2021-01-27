using System;

namespace GeBoCommon.AutoTranslation
{
    public interface IComponentTranslationContext
    {
        object Component { get; }
        string OriginalText { get; }
        string OverriddenTranslatedText { get; }
        void ResetBehaviour();
        void OverrideTranslatedText(string translation);
        void IgnoreComponent();
    }
}
