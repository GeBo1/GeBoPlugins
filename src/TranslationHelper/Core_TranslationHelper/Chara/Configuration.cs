using System.Diagnostics.CodeAnalysis;
using BepInEx.Logging;
using KKAPI.Chara;

#if (AI||HS2)
using AIChara;
#endif

namespace TranslationHelperPlugin.Chara
{
    internal static partial class Configuration
    {
        internal const string GUID = TranslationHelper.GUID + ".chara";
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static void Setup()
        {
            Logger.LogDebug($"{typeof(Configuration).FullName}.{nameof(Setup)}");
            var harmony = Hooks.SetupHooks();
            CharacterApi.RegisterExtraBehaviour<Controller>(GUID);
            GameSpecificSetup(harmony);


            //CharacterApi.CharacterReloaded += CharacterApi_CharacterReloaded;
            //ExtendedSave.CardBeingLoaded += ExtendedSave_CardBeingLoaded;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Temporary")]
        private static void TranslateChaFile(ChaFile file)
        {
            if (file != null && TranslationHelper.Instance.CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled)
            {
                file.StartMonitoredCoroutine(TranslationHelper.TranslateCardNames(file));
            }
        }

        /*
        private static void CharacterApi_CharacterReloaded(object dummy, CharaReloadEventArgs e)
        {
            TranslateChaFile(e.ReloadedCharacter?.chaFile);
        }
        */
        

        /*
        private static void ExtendedSave_CardBeingLoaded(ChaFile file)
        {
           TranslateChaFile(file);
        }
        */
    }
}
