using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace GeBoCommon.AutoTranslation
{
    public interface IAutoTranslationHelper
    {
        ManualLogSource Logger { get; }
        object DefaultCache { get; }

        bool TryTranslate(string untranslatedText, out string translatedText);
        bool TryTranslate(string untranslatedText, int scope, out string translatedText);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        void TranslateAsync(string untranslatedText, Action<ITranslationResult> onCompleted);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        void TranslateAsync(string untranslatedText, int scope, Action<ITranslationResult> onCompleted);

        void AddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope);
        void ReloadTranslations();
        bool IsTranslatable(string text);

        Dictionary<string, string> GetReplacements();

        string GetAutoTranslationsFilePath();

        Dictionary<string, string> GetTranslations();
        HashSet<string> GetRegisteredRegexes();
        HashSet<string> GetRegisteredSplitterRegexes();
    }
}
