using System;
using System.Collections;
using System.Collections.Generic;
using CharaCustom;
using GameLoadCharaFileSystem;
using GeBoCommon.Chara;
using HarmonyLib;
using KKAPI.Utilities;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Utils;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        private static readonly Limiter FileInfoLimiter = new Limiter(30, nameof(FileInfoLimiter));

        private static IEnumerator TranslateFileInfoCoroutine(Func<string> nameGetter, Func<CharacterSex> sexGetter,
            Action<string> nameSetter, params TranslationResultHandler[] handlers)
        {
            var origName = nameGetter();
            var innerHandlers = new List<TranslationResultHandler>
            {
                r =>
                {
                    FileInfoLimiter.EndImmediately();
                    if (!r.Succeeded) return;
                    nameSetter(r.TranslatedText);
                },
                Handlers.AddNameToCache(origName)
            };
            if (handlers.Length > 0) innerHandlers.AddRange(handlers);
            yield return FileInfoLimiter.Start().AppendCo(
                TranslationHelper.CardNameManager.TranslateCardName(origName,
                    new NameScope(sexGetter()),
                    innerHandlers.ToArray()));
        }

        private static void TranslateFileInfo(Func<string> nameGetter, Func<CharacterSex> sexGetter,
            Action<string> nameSetter, params TranslationResultHandler[] handlers)
        {
            TranslationHelper.Instance.StartCoroutine(TranslateFileInfoCoroutine(nameGetter, sexGetter, nameSetter,
                handlers));
        }

        internal static void TranslateFileInfo(GameCharaFileInfo info, params TranslationResultHandler[] handlers)
        {
            if (info == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            TranslateFileInfo(() => info.name, () => (CharacterSex)info.sex, n => info.SafeProc(i => i.name = n),
                handlers);
        }

        internal static void TranslateFileInfo(CustomCharaFileInfo info, params TranslationResultHandler[] handlers)
        {
            if (info == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            TranslateFileInfo(() => info.name, () => (CharacterSex)info.sex, n => info.SafeProc(i => i.name = n),
                handlers);
        }

        internal static void TranslateFileInfos(IEnumerable<GameCharaFileInfo> infos)
        {
            if (infos == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
        }

        private static void TranslateFileInfos(IEnumerable<CustomCharaFileInfo> infos)
        {
            if (infos == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCharaFileInfoAssist), nameof(GameCharaFileInfoAssist.CreateCharaFileInfoList))]
        internal static void CreateCharaFileInfoListPostfix(List<GameCharaFileInfo> __result)
        {
            TranslateFileInfos(__result);
        }

        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCharaFileScrollController), nameof(GameCharaFileScrollController.Init))]
        internal static void GameCharaFileInfoListPrefix(List<GameCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }

        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.CreateList))]
        internal static void CustomCharaFileInfoListPrefix(List<CustomCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }
    }
}
