using BepInEx.Logging;

namespace TranslationHelperPlugin.Translation
{
    internal static partial class Configuration
    {

        internal const string GUID = TranslationHelper.GUID + ".translationscope";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            Logger.LogInfo($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            GameSpecificSetup(harmony);
        }
    }
}
