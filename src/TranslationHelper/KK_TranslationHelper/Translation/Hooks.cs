using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ActionGame;
using ChaCustom;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        private static Coroutine _pointerEnterCoroutine;


        private static byte GuessSex(string club, string personality)
        {
            return (byte)(club == "帯刀" && string.IsNullOrEmpty(personality) ? 0 : 1);
        }

        // used in maker, starting new game, editing roster
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.AddList))]
        [HarmonyPatch(typeof(ClassRoomFileListCtrl), nameof(ClassRoomFileListCtrl.AddList))]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Hook")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        internal static void FileListCtrlAddListPrefix(CustomFileListCtrl __instance, int index, ref string name,
            string club, string personality, string fullpath)
        {
            if (Configuration.ListInfoNameTranslatedMap.TryGetValue(fullpath, out var tmpName))
            {
                name = tmpName;
                return;
            }

            if (TranslationHelper.Instance == null || string.IsNullOrEmpty(club) ||
                TranslationHelper.Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly)
            {
                return;
            }

            var sex = GuessSex(club, personality);


            void Handler(ITranslationResult result)
            {
                var newName = Configuration.ListInfoNameTranslatedMap[fullpath] = result.TranslatedText;

                var lstFileInfo = Traverse.Create(__instance)?.Field<List<CustomFileInfo>>("lstFileInfo")?.Value;
                var entry = lstFileInfo?.FirstOrDefault(x => x.index == index);

                if (entry == null) return;
                entry.name = newName;
            }

            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(name, new NameScope((CharacterSex)sex),
                    CardNameTranslationManager.CanForceSplitNameString(name), Handler));
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerEnter))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        public static void OnPointerEnterPostfix(CustomFileListCtrl __instance, GameObject obj)
        {
            if (obj == null) return;
            var component = obj.GetComponent<CustomFileInfoComponent>();
            if (component == null) return;
            var textDrawName = Traverse.Create(__instance)?.Field<Text>("textDrawName")?.Value;
            var name = component.info.name;
            if (Configuration.ListInfoNameTranslatedMap.TryGetValue(component.info.FullPath, out var tmpName))
            {
                component.info.name = tmpName;
                if (textDrawName != null) textDrawName.text = tmpName;
                return;
            }


            var sex = GuessSex(component.info.club, component.info.personality);

            void Handler(ITranslationResult result)
            {
                var newName = Configuration.ListInfoNameTranslatedMap[component.info.FullPath] = result.TranslatedText;
                component.info.name = newName;
                if (!result.Succeeded) return;
                if (textDrawName == null) return;
                textDrawName.text = newName;
            }

            if (textDrawName != null) textDrawName.text = name;

            _pointerEnterCoroutine = TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(name, new NameScope((CharacterSex)sex),
                    CardNameTranslationManager.CanForceSplitNameString(name),
                    Handler, _ => _pointerEnterCoroutine = null));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerExit))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        public static void OnPointerExitPrefix()
        {
            if (_pointerEnterCoroutine == null) return;
            TranslationHelper.Instance.StopCoroutine(_pointerEnterCoroutine);
            _pointerEnterCoroutine = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.ChangeItem))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        public static void ChangeItemPostfix(CustomFileListCtrl __instance, GameObject obj)
        {
            if (obj == null) return;
            var component = obj.GetComponent<CustomFileInfoComponent>();
            if (component == null) return;
            var selectDrawName = Traverse.Create(__instance)?.Field("selectDrawName");
            var name = component.info.name;
            if (Configuration.ListInfoNameTranslatedMap.TryGetValue(component.info.FullPath, out var tmpName))
            {
                component.info.name = tmpName;
                if (selectDrawName != null && selectDrawName.FieldExists()) selectDrawName.SetValue(tmpName);
                return;
            }


            var sex = GuessSex(component.info.club, component.info.personality);

            void Handler(ITranslationResult result)
            {
                var newName = result.TranslatedText;
                component.info.name = Configuration.ListInfoNameTranslatedMap[component.info.FullPath] = newName;
                if (!result.Succeeded) return;
                if (selectDrawName != null && selectDrawName.FieldExists())
                {
                    selectDrawName.SetValue(newName);
                }
            }

            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(name, new NameScope((CharacterSex)sex),
                    CardNameTranslationManager.CanForceSplitNameString(name), Handler));
        }
    }
}
