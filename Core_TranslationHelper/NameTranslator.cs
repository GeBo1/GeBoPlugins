using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Harmony;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace TranslationHelperPlugin
{
    internal class NameTranslator
    {
        internal static object LockObject = new object();
        internal static bool ForceScope;

        internal static ManualLogSource Logger => TranslationHelper.Logger;
        public bool TryTranslateName(string untranslatedText, out string translatedText)
        {
            Logger.LogFatal($"TryTranslateName: {untranslatedText}");
            if (!TranslationHelper.EnableOverrideNameTranslationScope.Value)
            {
                return GeBoAPI.Instance.AutoTranslationHelper.TryTranslate(untranslatedText, out translatedText);
            }

            lock (LockObject)
            {
                ForceScope = true;
                try
                {
                    return GeBoAPI.Instance.AutoTranslationHelper.TryTranslate(untranslatedText, out translatedText);
                }
                finally
                {
                    ForceScope = false;
                }
            }
        }

        [SuppressMessage("Naming", "RCS1047", Justification = "Inherited naming")]
        public void TranslateNameAsync(string untranslatedText, Action<ITranslationResult> onCompleted)
        {
            Logger.LogFatal($"TranslateNameAsync: {untranslatedText}");
            if (!TranslationHelper.EnableOverrideNameTranslationScope.Value)
            {
                GeBoAPI.Instance.AutoTranslationHelper.TranslateAsync(untranslatedText, onCompleted);
                return;
            }

            lock (LockObject)
            {
                ForceScope = true;
                try
                {
                    GeBoAPI.Instance.AutoTranslationHelper.TranslateAsync(untranslatedText, onCompleted);
                }
                finally
                {
                    ForceScope = false;
                }
            }
        }
    }
}
