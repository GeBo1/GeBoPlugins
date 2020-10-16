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
            // ReSharper disable once RedundantAssignment
            var start = Time.realtimeSinceStartup;
            if (infos == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
            Logger.DebugLogDebug(
                $"TranslateFileInfos(IEnumerable<GameCharaFileInfo>): {Time.realtimeSinceStartup - start:000.0000000000}");
        }

        private static void TranslateFileInfos(IEnumerable<CustomCharaFileInfo> infos)
        {
            // ReSharper disable once RedundantAssignment
            var start = Time.realtimeSinceStartup;
            if (infos == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
            Logger.DebugLogDebug(
                $"TranslateFileInfos(IEnumerable<CustomCharaFileInfo>): {Time.realtimeSinceStartup - start:000.0000000000}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCharaFileInfoAssist), nameof(GameCharaFileInfoAssist.CreateCharaFileInfoList))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void CreateCharaFileInfoListPostfix(List<GameCharaFileInfo> __result)
        {
            TranslateFileInfos(__result);
        }

        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCharaFileScrollController), nameof(GameCharaFileScrollController.Init))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void GameCharaFileInfoListPrefix(List<GameCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }

        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.CreateList))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void CustomCharaFileInfoListPrefix(List<CustomCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }
    }
}
