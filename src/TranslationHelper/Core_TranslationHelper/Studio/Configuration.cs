using System.Linq;
using BepInEx.Logging;
using KKAPI.Chara;
using KKAPI.Studio;
using Studio;
using TranslationHelperPlugin.Chara;
using IllusionStudio = Studio.Studio;
#if AI || HS2
using AIChara;

#endif


namespace TranslationHelperPlugin.Studio
{
    internal static partial class Configuration
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            if (!StudioAPI.InsideStudio) return;
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();

            CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;

            GameSpecificSetup(harmony);
        }

        private static void CharacterApi_CharacterReloaded(object sender, CharaReloadEventArgs e)
        {
            e?.ReloadedCharacter.SafeProcObject(UpdateTreeForChar);
        }

        internal static void UpdateTreeForChar(ChaControl chaControl)
        {
            UpdateTreeForChar(chaControl?.chaFile);
        }

        internal static void UpdateTreeForChar(ChaFile chaFile)
        {
            if (chaFile == null) return;

            void Handler(string fullName)
            {
                if (string.IsNullOrEmpty(fullName)) return;
                var match = Singleton<IllusionStudio>.Instance.dicInfo
                    .Where(e => e.Value is OCIChar ociChar && ociChar.charInfo.chaFile == chaFile)
                    .Select(e => e.Key).FirstOrDefault();
                if (match == null) return;
                match.textName = fullName;
            }

            chaFile.TranslateFullName(Handler);
        }
    }
}
