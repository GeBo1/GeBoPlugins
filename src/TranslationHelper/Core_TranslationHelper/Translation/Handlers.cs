using System;
using GeBoCommon.AutoTranslation;

namespace TranslationHelperPlugin.Translation
{
    // ReSharper disable once PartialTypeWithSinglePart
    public static partial class Handlers
    {
        public static TranslationResultHandler CallbackWrapper(Action<string> callback, string failureValue = null)
        {
            void Handler(ITranslationResult result)
            {
                callback(result.Succeeded ? result.TranslatedText : failureValue);
            }

            return Handler;
        }

        public static TranslationResultHandler FileInfoCacheHandler(NameScope scope, string path, string originalName)
        {
            void Handler(ITranslationResult result)
            {
                if (!result.Succeeded || string.IsNullOrEmpty(path) ||
                    TranslationHelper.NameStringComparer.Equals(result.TranslatedText, originalName)) return;
                CharaFileInfoTranslationManager.CacheRecentTranslation(scope, path, result.TranslatedText);
            }

            return Handler;
        }
    }
}
