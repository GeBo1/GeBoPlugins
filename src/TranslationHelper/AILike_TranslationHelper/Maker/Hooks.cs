using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CharaCustom;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using HarmonyLib;
using KKAPI.Maker;
using UnityEngine.UI;

namespace TranslationHelperPlugin.Maker
{
    internal partial class Hooks
    {
        private static bool _pointerCallbackRegistered;
        private static readonly HashSet<Text> PointerCallbackActiveTexts = new HashSet<Text>();


        private static void SelectTextCallback(IComponentTranslationContext context)
        {
            if (!_pointerCallbackRegistered ||!MakerAPI.InsideMaker) return;
            // ReSharper disable once InvertIf
            if (ComponentTranslationHelpers.TryTranslateFullName(context,
                t => t != null && PointerCallbackActiveTexts.Contains(t),
                () => null, (CharacterSex)MakerAPI.GetMakerSex()))
            {
                context.IgnoreComponent();
            }
        }

        private static void EnableSelectTextHandling()
        {
            if (_pointerCallbackRegistered) return;
            GeBoAPI.Instance.AutoTranslationHelper.RegisterOnTranslatingCallback(SelectTextCallback);
            _pointerCallbackRegistered = true;
        }

        private static void DisableSelectTextHandling()
        {
            if (GeBoAPI.Instance == null) return;
            GeBoAPI.Instance.AutoTranslationHelper.UnregisterOnTranslatingCallback(SelectTextCallback);
            _pointerCallbackRegistered = false;
        }

        internal static void ResetTranslatingCallbacks()
        {
            ResetSelectTextHandling();
        }

        private static void ResetSelectTextHandling()
        {
            PointerCallbackActiveTexts.Clear();
            DisableSelectTextHandling();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomCharaScrollController), "OnPointerEnter")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static void OnPointerEnterPatch(Text ___text)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled || ___text == null ||
                ___text.name != "SelectText") return;
            PointerCallbackActiveTexts.Add(___text);
            EnableSelectTextHandling();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaScrollController), "OnPointerExit")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static void OnPointerExitPatch(Text ___text)
        {
            PointerCallbackActiveTexts.Remove(___text);
            DisableSelectTextHandling();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.CreateList))]
        internal static void CreateListPatch()
        {
            ResetSelectTextHandling();
        }
    }
}
