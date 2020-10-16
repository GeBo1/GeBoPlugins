using BepInEx.Logging;

namespace TranslationHelperPlugin.Translation
{
    // ReSharper disable once PartialTypeWithSinglePart
    internal static partial class Configuration
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            GameSpecificSetup(harmony);
        }
    }
}
