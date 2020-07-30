using System;
using System.Diagnostics.CodeAnalysis;

namespace GeBoCommon.AutoTranslation
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public delegate bool TryTranslateDelegate(string untranslatedText, out string translatedText);

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public delegate void TranslateAsyncDelegate(string untranslatedText, Action<ITranslationResult> onCompleted);

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public delegate void AddTranslationToCacheDelegate(string key, string value, bool persistToDisk,
        int translationType, int scope);

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public delegate void ReloadTranslationsDelegate();

    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public delegate bool IsTranslatableDelegate(string text);
}
