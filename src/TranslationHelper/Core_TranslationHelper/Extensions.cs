using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;

namespace TranslationHelperPlugin
{
    internal static class Extensions
    {
        private static ManualLogSource Logger => TranslationHelper.Logger;

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Crash protection")]
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
                    Logger.LogError($"executing {handler.Method}: {err} {err.Message}");
                }
            }
        }
    }
}
