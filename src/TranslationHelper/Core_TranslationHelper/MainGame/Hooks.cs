using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using HarmonyLib;

namespace TranslationHelperPlugin.MainGame
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
