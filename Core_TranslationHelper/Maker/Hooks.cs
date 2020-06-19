using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;

#if AI||HS2
using CharaCustom;
#endif

namespace TranslationHelperPlugin.Maker
{
    internal partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static Harmony SetupHooks()
        {
            return HarmonyWrapper.PatchAll(typeof(Hooks));
        }

#if EC || KK
        /*
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustom.CustomCharaFile), "Initialize")]
        internal static void CustomScenePreHook() => TranslationHelper.AlternateLoadEventsEnabled = true;
        */

        /*
        [HarmonyPostfix, HarmonyPatch(typeof(ChaCustom.CustomCharaFile), "Initialize")]
        internal static void CustomScenePostHook() => TranslationHelper.AlternateLoadEventsEnabled = false;
        */
#else
        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaFileInfoAssist), "AddList")]
        internal static void LoadCharacterListPrefix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = true;
        }
        */

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaFileInfoAssist), "AddList")]
        internal static void LoadCharacterListPostfix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = false;
        }
        */

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsO_CharaLoad), "UpdateCharasList")]
        internal static void CvsO_CharaLoadUpdateCharasListPrefix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = true;
        }
        */

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CvsO_CharaLoad), "UpdateCharasList")]
        internal static void CvsO_CharaLoadUpdateCharasListPostfix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = false;
        }
        */

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CvsO_CharaSave), "UpdateCharasList")]
        internal static void CvsO_CharaSaveUpdateCharasListPrefix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = true;
        }
        */

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CvsO_CharaSave), "UpdateCharasList")]
        internal static void CvsO_CharaSaveUpdateCharasListPostfix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = false;
        }
        */
#endif
    }
}
