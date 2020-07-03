using BepInEx.Logging;
using KKAPI.Studio;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Configuration
    {

        internal const string GUID = TranslationHelper.GUID + ".maingame";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (StudioAPI.InsideStudio) return;
            Logger.LogInfo($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();



            GameSpecificSetup(harmony);
        }
    }
}
