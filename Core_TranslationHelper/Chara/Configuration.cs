using BepInEx.Logging;
using KKAPI.Chara;

namespace TranslationHelperPlugin.Chara
{
    internal static partial class Configuration
    {
        internal const string GUID = TranslationHelper.GUID + ".chara";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            Logger.LogInfo($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();
            CharacterApi.RegisterExtraBehaviour<Controller>(GUID);
            GameSpecificSetup(harmony);
        }
    }
}
