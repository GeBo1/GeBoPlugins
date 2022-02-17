using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CharaCustom;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
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
            if (!_pointerCallbackRegistered || !MakerAPI.InsideMaker) return;
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
            if (!GeBoAPI.Instance.SafeProc(g =>
                    g.AutoTranslationHelper.RegisterOnTranslatingCallback(SelectTextCallback)))
            {
                return;
            }

            _pointerCallbackRegistered = true;
        }

        private static void DisableSelectTextHandling()
        {
            if (!GeBoAPI.Instance.SafeProc(g =>
                    g.AutoTranslationHelper.UnregisterOnTranslatingCallback(SelectTextCallback)))
            {
                return;
            }

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
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.OnPointerEnter))]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static void OnPointerEnterPatch(Text ___text)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled || ___text == null ||
                ___text.name != "SelectText")
            {
                return;
            }

            try
            {
                PointerCallbackActiveTexts.Add(___text);
                EnableSelectTextHandling();
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(OnPointerEnterPatch));
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.OnPointerExit))]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal static void OnPointerExitPatch(Text ___text)
        {
            try
            {
                PointerCallbackActiveTexts.Remove(___text);
                DisableSelectTextHandling();
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(OnPointerExitPatch));
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomCharaScrollController), nameof(CustomCharaScrollController.CreateList))]
        internal static void CreateListPatch()
        {
            try
            {
                ResetSelectTextHandling();
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(CreateListPatch));
            }
        }
    }
}
