using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;

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
                    handler(result);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    Logger.LogException(err, $"{nameof(CallHandlers)}: Error executing {handler.Method}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }
    }
}
