using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using UnityEngine;

namespace TranslationHelperPlugin
{
    internal static class Extensions
    {
        private static ManualLogSource Logger => TranslationHelper.Logger;
        internal static void CallHandlers(this IEnumerable<TranslationResultHandler> handlers, ITranslationResult result)
        {
            var count = 0;
            foreach (var handler in handlers)
            {
                count++;
                try
                {
                    handler(result);
                }
                catch (Exception err)
                {
                    Logger.LogError($"executing {handler.Method}: {err} {err.Message}");
                }
            }
        }
    }
}
