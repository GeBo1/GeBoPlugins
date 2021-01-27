﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Maker;

#if AI||HS2
using AIChara;
#endif

namespace TranslationHelperPlugin.Chara
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static Harmony SetupHooks()
        {
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));
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

        [Obsolete]
        [UsedImplicitly]
        private static void AlternateReload(ChaFile file)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
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

        [HarmonyPostfix]
#if KK
        [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(string), typeof(bool), typeof(bool))]
#else
        [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(string), typeof(int), typeof(bool), typeof(bool))]
#endif
        private static void ChaFileLoadFilePostfix(ChaFile __instance, string path, bool __result)
        {
            if (!__result ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;

            if (!MakerAPI.CharaListIsLoading && !Configuration.TrackCharaFileControlPaths) return;

            __instance.GetTranslationHelperController().SafeProc(c => c.FullPath = path);
            Configuration.TrackCharaFileControlPath(__instance, path);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        private static void ChaFileControlLoadCharaFilePostfix(ChaFileControl __instance, string filename,
            bool __result)
        {
            if (!__result || __instance == null || string.IsNullOrEmpty(filename) ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled ||
                (!MakerAPI.CharaListIsLoading && !Configuration.TrackCharaFileControlPaths)) return;
#if HS2||AI
            if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
#endif

            try
            {
                filename = PathUtils.NormalizePath(filename);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception err)
            {
                Logger.LogDebug($"Unable to normalize filename '{filename}', will not track card path: {err}");
                return;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            __instance.GetTranslationHelperController().SafeProc(c => c.FullPath = filename);

            Configuration.TrackCharaFileControlPath(__instance, filename);
        }


#if KK
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool))]
#else
        [HarmonyPostfix, HarmonyPatch(typeof(ChaFile), "SaveFile", typeof(BinaryWriter), typeof(bool), typeof(int))]
#endif
        private static void ChaFile_SaveFile_Postfix(ChaFile __instance)
        {
            // ReSharper disable once UseNullPropagation -- Unity
            if (__instance == null) return;
            __instance.GetTranslationHelperController()
                .SafeProcObject(c => c.OnCardSaveComplete(KoikatuAPI.GetCurrentGameMode()));
        }
    }
}
