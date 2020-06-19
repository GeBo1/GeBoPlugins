using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Harmony;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using HarmonyLib;
using KKAPI;
using TranslationHelperPlugin.Chara;
using KKAPI.Maker;
using UnityEngine;
#if AI||HS2
using AIChara;
#endif

namespace TranslationHelperPlugin.Chara
{
    internal partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static Harmony SetupHooks()
        {
            var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
            return harmony;

        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ExtendedSave), "CardReadEvent", typeof(ChaFile))]
        internal static void ExtendedSaveCardReadEventPostfix(ChaFile file)
        {
            if (file == null || !TranslationHelper.UsingAlternateLoadEvents) return;
            AlternateReload(file);
        }
        */

        private static void AlternateReload(ChaFile file)
        {
            var controller = file.GetTranslationHelperController();
            if (controller != null)
            {
                controller.OnAlternateReload();
            }
            else
            {
                TranslationHelper.Instance.StartCoroutine(TranslationHelper.CardNameManager.TranslateCardNames(file));
            }

        }

        /*
        [HarmonyPrefix]
#if KK
        [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
        [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
        internal static void ChaFileLoadFilePrefix() => TranslationHelper.AlternateLoadEventsEnabled = true;
        */

        /*
        [HarmonyPostfix]
#if KK
        [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
        [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
        internal static void ChaFileLoadFilePostfix() => TranslationHelper.AlternateLoadEventsEnabled = false;
        */

#if KK
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
        internal static void ChaFile_SaveFile_Postfix(ChaFile __instance)
        {
            if (__instance == null) return;
            __instance.GetTranslationHelperController()
                .SafeProcObject(c => c.OnCardSaveComplete(KoikatuAPI.GetCurrentGameMode()));
        }
    }
}
