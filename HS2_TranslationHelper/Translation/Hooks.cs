using System;
using System.Collections.Generic;
using AIChara;
using CharaCustom;
using ExtensibleSaveFormat;
using GameLoadCharaFileSystem;
using GeBoCommon.Chara;
using HarmonyLib;
using Illusion;
using TranslationHelperPlugin.Chara;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        internal static bool HookLoadCharaFile = false;
        private static void TranslateFileInfo(Func<string> nameGetter, Func<CharacterSex> sexGetter, Action<string> nameSetter, params TranslationResultHandler[] handlers)
        {
            var origName = nameGetter();
            var innerHandlers = new List<TranslationResultHandler>
            {
                r =>
                {
                    if (!r.Succeeded) return;
                    nameSetter(r.TranslatedText);
                },
                Chara.Handlers.AddNameToCache(origName)
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
        private static void CreateCharaFileInfoListPostfix(List<GameCharaFileInfo> __result)
        {
            TranslateFileInfos(__result);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCharaFileScrollController), nameof(GameCharaFileScrollController.Init))]
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), nameof(LobbyCharaSelectInfoScrollController.Init))]
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController1),
            nameof(LobbyCharaSelectInfoScrollController1.Init))]
        private static void GameCharaFileInfoListPrefix(List<GameCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.CreateList))]
        private static void CustomCharaFileInfoListPrefix(List<CustomCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameCharaFileScrollController), "OnValueChanged")]
        [HarmonyPatch(typeof(CustomCharaScrollViewInfo), nameof(CustomCharaScrollViewInfo.SetData))]
        private static void UpdateScrollDataPrefix(CustomCharaScrollController.ScrollData _data)
        {
            if (_data == null) return;
            void Handler(ITranslationResult result)
            {
                if (!result.Succeeded) return;
                _data.info.name = result.TranslatedText;
            }

            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(_data.info.name,
                    new NameScope((CharacterSex)_data.info.sex), Handler));
        }


        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaScrollViewInfo), nameof(CustomCharaScrollViewInfo.SetData))]
        private static void CustomCharaScrollViewInfoSetDataPrefix(CustomCharaScrollViewInfo __instance, CustomCharaScrollController.ScrollData _data)
        {
            if (__instance == null || _data == null) return;

            void Handler(ITranslationResult result)
            {
                if (!result.Succeeded) return;
                _data.info.name = result.TranslatedText;
            }

            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(_data.info.name,
                    new NameScope((CharacterSex)_data.info.sex), Handler));
        }
        */


        
       
    }
}
