using HarmonyLib;

namespace TranslationHelperPlugin.Chara
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Hooks.KK_SetupHooks(harmony);
        }
    }
}
