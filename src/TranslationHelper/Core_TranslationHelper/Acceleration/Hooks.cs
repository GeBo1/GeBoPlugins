using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;

namespace TranslationHelperPlugin.Acceleration
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Hooks
    {
        [UsedImplicitly]
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }
    }
}
