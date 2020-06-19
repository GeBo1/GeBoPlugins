using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;

namespace TranslationHelperPlugin
{
    internal static class Extensions
    {
        private static ManualLogSource Logger => TranslationHelper.Logger;
        internal static void CallHandlers(this IEnumerable<TranslationResultHandler> handlers, ITranslationResult result)
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler(result);
                }
                catch (Exception err)
                {
                    Logger.LogError($"executing {handler.Method}: {err.Message}");
                }
            }
        }
    }
}
