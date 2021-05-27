using BepInEx.Logging;

namespace GeBoCommon
{
    internal static class Common
    {
        private static ManualLogSource _currentLogger;

        internal static ManualLogSource CurrentLogger =>
            _currentLogger ?? (_currentLogger = new ManualLogSource(nameof(GeBoCommon)));

        internal static void SetCurrentLogger(ManualLogSource logger)
        {
            _currentLogger = logger;
        }
    }
}
