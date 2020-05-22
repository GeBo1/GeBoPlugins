using BepInEx.Logging;
using System;
using System.Collections.Generic;

namespace GeBoCommon.AutoTranslation.Implementation
{
    internal class StubAutoTranslationHelper : AutoTranslationHelperBase, IAutoTranslationHelper
    {
        public object DefaultCache => null;
        ManualLogSource IAutoTranslationHelper.Logger => Logger;

        public Dictionary<string, string> GetReplacements() => new Dictionary<string, string>();

        public string GetAutoTranslationsFilePath() => null;

        public Dictionary<string, string> GetTranslations() => new Dictionary<string, string>();

        public HashSet<string> GetRegisteredRegexes() => new HashSet<string>();

        public HashSet<string> GetRegisteredSplitterRegexes() => new HashSet<string>();

        public bool TryTranslate(string untranslatedText, out string translatedText) => FallbackTryTranslate(untranslatedText, out translatedText);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        public void TranslateAsync(string untranslatedText, Action<ITranslationResult> onCompleted)
        {
            FallbackTranslateAsync(untranslatedText, onCompleted, new TranslationResult("Translator Plugin not available"));
        }

        public void AddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope)
        {
            FallbackAddTranslationToCache(key, value, persistToDisk, translationType, scope);
        }

        public void ReloadTranslations() { }

        public bool IsTranslatable(string text) => FallbackIsTranslatable(text);

        public class TranslationResult : ITranslationResult
        {
            internal TranslationResult(string result)
            {
                ErrorMessage = result;
            }

            public bool Succeeded => false;

            public string TranslatedText => string.Empty;

            public string ErrorMessage { get; }
        }
    }
}
