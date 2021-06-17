using System;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;

namespace TranslationHelperPlugin.Translation
{
    internal class NameTranslator
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        // ReSharper disable once MemberCanBeMadeStatic.Local
#pragma warning disable CA1822 // Mark members as static
        private IAutoTranslationHelper AutoTranslationHelper => GeBoAPI.Instance.AutoTranslationHelper;
#pragma warning restore CA1822 // Mark members as static

        public bool TryTranslateName(string untranslatedText, NameScope nameScope, out string translatedText)
        {
            return AutoTranslationHelper.TryTranslate(untranslatedText, nameScope.TranslationScope,
                out translatedText);
        }

        /*
        public bool TryTranslateName(string untranslatedText, out string translatedText)
        {
            return TryTranslateName(untranslatedText, NameType.General, out translatedText);
        }
        */

        public void TranslateNameAsync(string untranslatedText, NameScope nameScope,
            Action<ITranslationResult> onCompleted)
        {
            AutoTranslationHelper.TranslateAsync(untranslatedText, nameScope.TranslationScope,
                onCompleted);
        }

        /*
        public void TranslateNameAsync(string untranslatedText, Action<ITranslationResult> onCompleted)
        {
            TranslateNameAsync(untranslatedText, NameType.General, onCompleted);
        }
        */
    }
}
