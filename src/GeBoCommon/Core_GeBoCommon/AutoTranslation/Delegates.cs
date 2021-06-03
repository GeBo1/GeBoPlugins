using System;
using JetBrains.Annotations;

namespace GeBoCommon.AutoTranslation
{
    [PublicAPI]
    public delegate bool TryTranslateDelegate(string untranslatedText, out string translatedText);

    [PublicAPI]
    public delegate void TranslateAsyncDelegate(string untranslatedText, Action<ITranslationResult> onCompleted);

    [PublicAPI]
    public delegate void AddTranslationToCacheDelegate(string key, string value, bool persistToDisk,
        int translationType, int scope);

    [PublicAPI]
    public delegate void ReloadTranslationsDelegate();

    [PublicAPI]
    public delegate bool IsTranslatableDelegate(string text);

    [PublicAPI]
    public delegate bool TestStringDelegate(string text);

    [PublicAPI]
    public delegate string TransformStringDelegate(string text);
}
