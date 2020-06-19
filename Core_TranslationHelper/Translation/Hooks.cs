using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace TranslationHelperPlugin.Translation
{
    internal static partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static Harmony SetupHooks()
        {
            return HarmonyWrapper.PatchAll(typeof(Hooks));
        }
    }
}
