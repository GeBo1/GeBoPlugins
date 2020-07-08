using System.Collections.Generic;
using GeBoCommon;
using GeBoCommon.Chara;
using HarmonyLib;
using KKAPI.MainGame;
using TMPro;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.UI;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Hooks
    {
        private static IEnumerable<T> FindChildComponents<T>(Component component, string fieldName,
            params string[] paths) where T : Object
        {
            var result = Traverse.Create(component)?.Field<T>(fieldName)?.Value;
            if (result != null) yield return result;

            foreach (var path in paths)
            {
                var transform = component.transform.Find(path);
                if (transform == null) continue;

                result = transform.GetComponent<T>();
                if (result != null) yield return result;
            }
        }

        private static bool ShouldProcess(Component component, ChaFile chaFile)
        {
            return component != null && chaFile != null &&
                   TranslationHelper.Instance.CurrentCardLoadTranslationMode >= CardLoadTranslationMode.CacheOnly &&
                   (!chaFile.TryGetTranslationHelperController(out var controller) ||
                    (!controller.TranslationInProgress && !controller.IsTranslated));
        }

        // Student Cards in roster/new game and Free H selection
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StudentCardControlComponent), nameof(StudentCardControlComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        [HarmonyPatch(typeof(CharaHInfoComponent), nameof(CharaHInfoComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        internal static void RosterSetCharaInfoPrefix(Component __instance, ChaFileControl chaFileCtrl)
        {
            if (!ShouldProcess(__instance, chaFileCtrl) ||
                !GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(chaFileCtrl.GetFullName()))
            {
                return;
            }

            chaFileCtrl.StartMonitoredCoroutine(TranslationHelper.CardNameManager.TranslateCardNames(chaFileCtrl));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StudentCardControlComponent), nameof(StudentCardControlComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        [HarmonyPatch(typeof(CharaHInfoComponent), nameof(CharaHInfoComponent.SetCharaInfo),
            typeof(ChaFileControl))]
        internal static void RosterSetCharaInfoPostfix(Component __instance, ChaFileControl chaFileCtrl)
        {
            if (!ShouldProcess(__instance, chaFileCtrl)) return;

            void UpdateDisplayedCard(string name)
            {
                foreach (var textName in FindChildComponents<Text>(__instance, "textName",
                    "resize/text/name", "resize/image/chara"))
                {
                    textName.text = name;
                }
            }

            chaFileCtrl.TranslateFullName(UpdateDisplayedCard);

            FreeHUpdateUI();
        }

        private static void FreeHUpdateUI()
        {
            // for 3P the character names show in one extra place that the standard check doesn't cover.
            // easiest to just update each time.
            var freeHScene = Object.FindObjectOfType<FreeHScene>();
            if (freeHScene == null) return;
            var member = Traverse.Create(freeHScene).Field<FreeHScene.Member>("member")?.Value;
            if (member == null) return;

            void Update3P(string fieldName, string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                var field = Traverse.Create(freeHScene).Field<TextMeshProUGUI>(fieldName)?.Value;
                if (field == null) return;
                field.text = value;
            }

            var heroineMap = new Dictionary<string, SaveData.Heroine>();
            if (member.resultHeroine.HasValue && member.resultHeroine.Value != null)
            {
                heroineMap["textFemaleName1"] = member.resultHeroine.Value;
            }

            if (member.resultPartner.HasValue && member.resultPartner.Value != null)
            {
                heroineMap["textFemaleName2"] = member.resultPartner.Value;
            }

            foreach (var entry in heroineMap)
            {
                foreach (var heroineChaFile in entry.Value.GetRelatedChaFiles())
                {
                    heroineChaFile.TranslateFullName(
                        translated => Update3P(entry.Key, translated));
                }
            }
        }
    }
}
