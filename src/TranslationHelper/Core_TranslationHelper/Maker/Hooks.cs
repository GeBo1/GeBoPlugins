using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using HarmonyLib;

#if AI||HS2

#endif

namespace TranslationHelperPlugin.Maker
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
