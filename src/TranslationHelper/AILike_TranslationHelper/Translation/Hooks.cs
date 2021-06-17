using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CharaCustom;
using GameLoadCharaFileSystem;
using GeBoCommon.Utilities;
using HarmonyLib;
using TranslationHelperPlugin.Utils;
using UnityEngine;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        internal static void TranslateFileInfo(GameCharaFileInfo info, params TranslationResultHandler[] handlers)
        {
            if (info == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            var wrapper = CharaFileInfoWrapper.CreateWrapper(info);
            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.Instance.FileInfoTranslationManager.TranslateFileInfo(wrapper, handlers));
        }

        internal static void TranslateFileInfo(CustomCharaFileInfo info, params TranslationResultHandler[] handlers)
        {
            if (info == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            var wrapper = CharaFileInfoWrapper.CreateWrapper(info);
            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.Instance.FileInfoTranslationManager.TranslateFileInfo(wrapper, handlers));
        }

        internal static void TranslateFileInfos(IEnumerable<GameCharaFileInfo> infos)
        {
            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;

            if (infos == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
            Logger.DebugLogDebug(
                $"TranslateFileInfos(IEnumerable<GameCharaFileInfo>): {Time.realtimeSinceStartup - start:000.0000000000}");
        }

        private static void TranslateFileInfos(IEnumerable<CustomCharaFileInfo> infos)
        {
            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;
            if (infos == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
            Logger.DebugLogDebug(
                $"TranslateFileInfos(IEnumerable<CustomCharaFileInfo>): {Time.realtimeSinceStartup - start:000.0000000000}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCharaFileInfoAssist), nameof(GameCharaFileInfoAssist.CreateCharaFileInfoList))]
        internal static void CreateCharaFileInfoListPostfix(List<GameCharaFileInfo> __result)
        {
            try
            {
                TranslateFileInfos(__result);
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(CreateCharaFileInfoListPostfix));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCharaFileScrollController), nameof(GameCharaFileScrollController.Init))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
        internal static void GameCharaFileInfoListPrefix(List<GameCharaFileInfo> _lst)
        {
            try
            {
                TranslateFileInfos(_lst);
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(GameCharaFileInfoListPrefix));
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.CreateList))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
        internal static void CustomCharaFileInfoListPrefix(List<CustomCharaFileInfo> _lst)
        {
            try
            {
                TranslateFileInfos(_lst);
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(CustomCharaFileInfoListPrefix));
            }
        }
    }
}
