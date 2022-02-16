using System.Diagnostics;
using System.Text;
using BepInEx.Logging;
using JetBrains.Annotations;
#if GEBO_COMMON_FULL
using GeBoCommon.Utilities;
#endif


namespace GeBoCommon
{
    internal static class Common
    {
        private static ManualLogSource _currentLogger;

        internal static ManualLogSource CurrentLogger =>
            _currentLogger ?? (_currentLogger = new ManualLogSource(nameof(GeBoCommon)));

        [UsedImplicitly]
        internal static void SetCurrentLogger(ManualLogSource logger)
        {
            _currentLogger = logger;
        }

        internal static StringBuilder RequestStringBuilder()
        {
#if GEBO_COMMON_FULL
            return StringBuilderPool.Get();
#else
            return new StringBuilder();
#endif
        }

        [Conditional("GEBO_COMMON_FULL")]
        // ReSharper disable once UnusedParameter.Global
        internal static void ReleaseStringBuilder(StringBuilder stringBuilder)
        {
#if GEBO_COMMON_FULL
            StringBuilderPool.Release(stringBuilder);
#endif
        }
    }
}
