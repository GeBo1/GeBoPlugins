using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using HarmonyLib;

namespace TranslationHelperPlugin
{
    internal static class Extensions
    {
        private static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void CallHandlers(this IEnumerable<TranslationResultHandler> handlers,
            ITranslationResult result)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    Logger.DebugLogDebug($"{nameof(CallHandlers)}: {handler.Method.FullDescription()}");
                    handler(result);
                }
                catch (Exception err)
                {
                    Logger.LogException(err, $"{nameof(CallHandlers)}: Error executing {handler.Method}");
                }
            }
        }
    }
}
