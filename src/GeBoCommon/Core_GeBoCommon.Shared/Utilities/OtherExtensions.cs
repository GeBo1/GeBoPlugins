using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Logging;
using BepInLogLevel = BepInEx.Logging.LogLevel;

namespace GeBoCommon.Utilities
{
    public static class OtherExtensions
    {
        public static IEnumerable<KeyValuePair<int, T>> Enumerate<T>(this IEnumerable<T> array)
        {
            return array.Select((item, index) => new KeyValuePair<int, T>(index, item));
        }

        [Conditional("DEBUG")]
        public static void DebugLogDebug(this ManualLogSource logger, object obj)
        {
            logger.LogDebug(obj);
        }

        public static void LogErrorMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Error, obj);
        }

        public static void LogFatalMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Fatal, obj);
        }

        public static void LogInfoMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Info, obj);
        }

        public static void LogWarningMessage(this ManualLogSource logger, object obj)
        {
            logger.Log(BepInLogLevel.Message | BepInLogLevel.Warning, obj);
        }
    }
}
