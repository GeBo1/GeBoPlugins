using System;
using System.Collections.Generic;
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
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    Logger.LogError($"Error executing {handler.Method}: {err.Message}");
                    Logger.LogDebug(err);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }
    }
}
