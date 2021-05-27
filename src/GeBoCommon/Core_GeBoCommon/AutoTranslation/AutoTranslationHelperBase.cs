using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeBoCommon.AutoTranslation
{
    internal class AutoTranslationHelperBase
    {
        protected ManualLogSource Logger => Common.CurrentLogger;

        protected bool FallbackTryTranslate(string _, out string translatedText)
        {
            translatedText = string.Empty;
            return false;
        }

        protected void FallbackAddTranslationToCache(string key, string value, bool persistToDisk, int translationType,
            int scope)
        {
            _ = persistToDisk || translationType == scope || string.IsNullOrEmpty(key ?? value);
        }

        protected void FallbackTranslateAsync(string _, Action<ITranslationResult> onCompleted,
            ITranslationResult cannedResult)
        {
            onCompleted(cannedResult);
        }

        protected int FallbackGetCurrentTranslationScope()
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

        protected string NoOpTransformStringDelegate(string text)
        {
            return text;
        }

        protected bool AlwaysFalseTestStringDelegate(string text)
        {
            return false;
        }
    }
}
