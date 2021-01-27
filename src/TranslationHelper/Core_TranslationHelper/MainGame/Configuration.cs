using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using KKAPI.Studio;

namespace TranslationHelperPlugin.MainGame
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal const string GUID = TranslationHelper.GUID + ".maingame";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (StudioAPI.InsideStudio) return;
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            GameSpecificSetup(harmony);
        }
    }
}
