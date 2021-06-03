using BepInEx.Logging;
using JetBrains.Annotations;

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
    }
}
