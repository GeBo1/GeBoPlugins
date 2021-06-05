using HarmonyLib;

namespace TranslationHelperPlugin.Chara
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Hooks.KKS_SetupHooks(harmony);
        }
    }
}
