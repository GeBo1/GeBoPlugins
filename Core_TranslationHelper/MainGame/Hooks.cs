using BepInEx.Logging;
using HarmonyLib;

namespace TranslationHelperPlugin.MainGame
{
    // ReSharper disable once PartialTypeWithSinglePart
    partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
