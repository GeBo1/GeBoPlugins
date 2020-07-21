using System;
using System.Collections.Generic;
using CharaCustom;
using GameLoadCharaFileSystem;
using GeBoCommon.Chara;
using HarmonyLib;
using TranslationHelperPlugin.Chara;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        private static void TranslateFileInfo(Func<string> nameGetter, Func<CharacterSex> sexGetter,
            Action<string> nameSetter, params TranslationResultHandler[] handlers)
        {
            var origName = nameGetter();
            var innerHandlers = new List<TranslationResultHandler>
            {
                r =>
                {
                    if (!r.Succeeded) return;
                    nameSetter(r.TranslatedText);
                },
                Handlers.AddNameToCache(origName)
            };
            if (handlers.Length > 0) innerHandlers.AddRange(handlers);
            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(origName,
                    new NameScope(sexGetter()),
                    innerHandlers.ToArray()));
        }

        internal static void TranslateFileInfo(GameCharaFileInfo info, params TranslationResultHandler[] handlers)
        {
            if (info == null) return;
            TranslateFileInfo(() => info.name, () => (CharacterSex)info.sex, n => info.name = n, handlers);
        }

        internal static void TranslateFileInfo(CustomCharaFileInfo info, params TranslationResultHandler[] handlers)
        {
            if (info == null) return;
            TranslateFileInfo(() => info.name, () => (CharacterSex)info.sex, n => info.name = n, handlers);
        }

        internal static void TranslateFileInfos(IEnumerable<GameCharaFileInfo> infos)
        {
            if (infos == null) return;
            foreach (var fileInfo in infos) TranslateFileInfo(fileInfo);
        }

        private static void TranslateFileInfos(IEnumerable<CustomCharaFileInfo> infos)
        {
            if (infos == null) return;
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
