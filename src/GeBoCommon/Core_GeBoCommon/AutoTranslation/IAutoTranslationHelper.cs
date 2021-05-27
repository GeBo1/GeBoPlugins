using System;
using System.Collections.Generic;
using BepInEx.Logging;
using JetBrains.Annotations;

namespace GeBoCommon.AutoTranslation
{
    [PublicAPI]
    public interface IAutoTranslationHelper
    {
        ManualLogSource Logger { get; }
        object DefaultCache { get; }

        bool TryTranslate(string untranslatedText, out string translatedText);
        bool TryTranslate(string untranslatedText, int scope, out string translatedText);

        void TranslateAsync(string untranslatedText, Action<ITranslationResult> onCompleted);

        void TranslateAsync(string untranslatedText, int scope, Action<ITranslationResult> onCompleted);

        void IgnoreTextComponent(object textComponent);

        void UnignoreTextComponent(object textComponent);

        void RegisterOnTranslatingCallback(Action<IComponentTranslationContext> context);

        void UnregisterOnTranslatingCallback(Action<IComponentTranslationContext> context);

        void AddTranslationToCache(string key, string value, bool persistToDisk, int translationType, int scope);
        void ReloadTranslations();
        bool IsTranslatable(string text);

        Dictionary<string, string> GetReplacements();

        string GetAutoTranslationsFilePath();

        Dictionary<string, string> GetTranslations();
        HashSet<string> GetRegisteredRegexes();
        HashSet<string> GetRegisteredSplitterRegexes();

        int GetCurrentTranslationScope();

        bool ContainsVariableSymbol(string text);
        bool IsRedirected(string text);
        string FixRedirected(string text);
        string MakeRedirected(string text);
    }
}
