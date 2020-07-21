using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;

namespace GeBoCommon.AutoTranslation.Implementation
{
    internal class StubAutoTranslationHelper : AutoTranslationHelperBase, IAutoTranslationHelper
    {
        public object DefaultCache => null;
        ManualLogSource IAutoTranslationHelper.Logger => Logger;

        public Dictionary<string, string> GetReplacements()
        {
            return new Dictionary<string, string>();
        }

        public string GetAutoTranslationsFilePath()
        {
            return null;
        }

        public Dictionary<string, string> GetTranslations()
        {
            return new Dictionary<string, string>();
        }

        public HashSet<string> GetRegisteredRegexes()
        {
            return new HashSet<string>();
        }

        public HashSet<string> GetRegisteredSplitterRegexes()
        {
            return new HashSet<string>();
        }

        public bool TryTranslate(string untranslatedText, out string translatedText)
        {
            return FallbackTryTranslate(untranslatedText, out translatedText);
        }

        public bool TryTranslate(string untranslatedText, int scope, out string translatedText)
        {
            return FallbackTryTranslate(untranslatedText, out translatedText);
        }

        [SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        public void TranslateAsync(string untranslatedText, Action<ITranslationResult> onCompleted)
        {
            FallbackTranslateAsync(untranslatedText, onCompleted,
                new TranslationResult("Translator Plugin not available"));
        }

        public void TranslateAsync(string untranslatedText, int scope, Action<ITranslationResult> onCompleted)
        {
            TranslateAsync(untranslatedText, onCompleted);
        }

        public void AddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope)
        {
            FallbackAddTranslationToCache(key, value, persistToDisk, translationType, scope);
        }

        public void ReloadTranslations() { }

        public bool IsTranslatable(string text)
        {
            return FallbackIsTranslatable(text);
        }

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
