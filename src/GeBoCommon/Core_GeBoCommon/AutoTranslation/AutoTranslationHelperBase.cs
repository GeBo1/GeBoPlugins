using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeBoCommon.AutoTranslation
{
    internal class AutoTranslationHelperBase
    {
        protected static ManualLogSource Logger => Common.CurrentLogger;

        protected static bool FallbackTryTranslate(string key, out string translatedText)
        {
            translatedText = string.Empty;
            _ = key;
            return false;
        }

        protected static void FallbackAddTranslationToCache(string key, string value, bool persistToDisk,
            int translationType,
            int scope)
        {
            _ = persistToDisk || translationType == scope || string.IsNullOrEmpty(key ?? value);
        }

        protected static void FallbackTranslateAsync(string key, Action<ITranslationResult> onCompleted,
            ITranslationResult cannedResult)
        {
            _ = key;
            onCompleted(cannedResult);
        }

        protected static int FallbackGetCurrentTranslationScope()
        {
            try
            {
                try
                {
                    return SceneManager.GetActiveScene().buildIndex;
                }
                catch
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    return Application.loadedLevel;
#pragma warning restore CS0618 // Type or member is obsolete
                }
            }
            catch
            {
                return -1;
            }
        }

        protected static string NoOpTransformStringDelegate(string text)
        {
            return text;
        }

        protected static bool AlwaysFalseTestStringDelegate(string text)
        {
            _ = text;
            return false;
        }
    }
}
