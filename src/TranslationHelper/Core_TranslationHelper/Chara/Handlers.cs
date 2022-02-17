using System.Diagnostics.CodeAnalysis;
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
            var capChaFile = chaFile;
            var capNameIndex = nameIndex;

            void UpdateCardNameHandler(ITranslationResult result)
            {
                if (capChaFile == null || !result.Succeeded || string.IsNullOrEmpty(result.TranslatedText) ||
                    TranslationHelper.NameStringComparer.Equals(capChaFile.GetName(capNameIndex),
                        result.TranslatedText))
                {
                    return;
                }

                capChaFile.SetTranslatedName(capNameIndex, result.TranslatedText);
            }

            return UpdateCardNameHandler;
        }


        public static TranslationResultHandler AddNameToAutoTranslationCache(string originalName,
            bool allowPersistToDisk = false)
        {
            var capOriginalName = originalName;
            var capAllowPersistToDisk = allowPersistToDisk;

            void AddNameToCacheHandler(ITranslationResult result)
            {
                if (!result.Succeeded || string.IsNullOrEmpty(result.TranslatedText) ||
                    result.TranslatedText == capOriginalName ||
                    TranslationHelper.Instance.CurrentCardLoadTranslationMode <
                    CardLoadTranslationMode.CacheOnly)
                {
                    return;
                }

                TranslationHelper.Instance.AddTranslatedNameToCache(capOriginalName, result.TranslatedText,
                    capAllowPersistToDisk);
            }

            return AddNameToCacheHandler;
        }
    }
}
