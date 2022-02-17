using System;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using JetBrains.Annotations;

namespace TranslationHelperPlugin.Translation
{
    internal class NameTranslator
    {
        [UsedImplicitly]
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private static IAutoTranslationHelper AutoTranslationHelper => GeBoAPI.Instance.AutoTranslationHelper;

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
