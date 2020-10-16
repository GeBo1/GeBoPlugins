using System;
// ReSharper disable UnusedMember.Global


namespace GeBoCommon.AutoTranslation
{
    public delegate bool TryTranslateDelegate(string untranslatedText, out string translatedText);
    public delegate void TranslateAsyncDelegate(string untranslatedText, Action<ITranslationResult> onCompleted);
    public delegate void AddTranslationToCacheDelegate(string key, string value, bool persistToDisk, int translationType, int scope);
    public delegate void ReloadTranslationsDelegate();
    public delegate bool IsTranslatableDelegate(string text);
}
