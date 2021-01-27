using System;
using JetBrains.Annotations;

namespace GeBoCommon.AutoTranslation
{
    [PublicAPI]
    public delegate bool TryTranslateDelegate(string untranslatedText, out string translatedText);

    [PublicAPI]
    public delegate void TranslateAsyncDelegate(string untranslatedText, Action<ITranslationResult> onCompleted);

    public delegate void AddTranslationToCacheDelegate(string key, string value, bool persistToDisk,
        int translationType, int scope);

    public delegate void ReloadTranslationsDelegate();

    [PublicAPI]
    public delegate bool IsTranslatableDelegate(string text);
}
