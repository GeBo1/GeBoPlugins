﻿using System.Diagnostics.CodeAnalysis;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;

#if AI||HS2
using AIChara;
#endif

namespace TranslationHelperPlugin.Chara
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    public static partial class Handlers
    {

        public static TranslationResultHandler UpdateCardName(ChaFile chaFile, int nameIndex)
        {
            void UpdateCardNameHandler(ITranslationResult result)
            {
                if (chaFile == null || !result.Succeeded || string.IsNullOrEmpty(result.TranslatedText) ||
                    chaFile.GetName(nameIndex) == result.TranslatedText) return;
                chaFile.SetTranslatedName(nameIndex, result.TranslatedText);
            }

            return UpdateCardNameHandler;
        }


        public static TranslationResultHandler AddNameToAutoTranslationCache(string originalName, bool allowPersistToDisk = false)
        {
            void AddNameToCacheHandler(ITranslationResult result)
            {
                if (!result.Succeeded || string.IsNullOrEmpty(result.TranslatedText) ||
                    result.TranslatedText == originalName ||
                    TranslationHelper.Instance.CurrentCardLoadTranslationMode <
                    CardLoadTranslationMode.CacheOnly) return;

                TranslationHelper.Instance.AddTranslatedNameToCache(originalName, result.TranslatedText,
                    allowPersistToDisk);

            }
            return AddNameToCacheHandler;
        }
    }
}
