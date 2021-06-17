using System;
using System.Collections.Generic;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace TranslationHelperPlugin.Studio
{
    internal partial class Hooks
    {
        private static bool _charaListHandlerRegistered;
        private static readonly HashSet<MonoBehaviour> CharaListsInProgress = new HashSet<MonoBehaviour>();

        // KK has separate firstname/lastname fields which allows for more accurate translation
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitFemaleList))]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitMaleList))]
        internal static void InitGenderListPrefix()
        {
            Translation.Configuration.LoadCharaFileMonitorEnabled = true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitFemaleList))]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitMaleList))]
        internal static void InitGenderListPostfix()
        {
            Translation.Configuration.LoadCharaFileMonitorEnabled = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitCharaList))]
        internal static void StudioInitCharaListPrefix(CharaList __instance)
        {
            try
            {
                if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
                if (CharaListsInProgress.Contains(__instance)) return;
                CharaListsInProgress.Add(__instance);
                EnableCharaListHandler();
            }

            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(StudioInitCharaListPrefix));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), nameof(CharaList.InitCharaList))]
        internal static void StudioInitCharaListPostfix(CharaList __instance)
        {
            try
            {
                CharaListsInProgress.Remove(__instance);
                if (CharaListsInProgress.Count == 0) DisableCharaListHandler();
            }

            catch (Exception err)
            {
                Logger.LogException(err, __instance, nameof(StudioInitCharaListPostfix));
            }
        }

        internal static void ResetTranslatingCallbacks()
        {
            DisableCharaListHandler();
        }

        private static void EnableCharaListHandler()
        {
            if (_charaListHandlerRegistered) return;
            GeBoAPI.Instance.AutoTranslationHelper.RegisterOnTranslatingCallback(CharaListHandler);
            _charaListHandlerRegistered = true;
        }

        private static void DisableCharaListHandler()
        {
            CharaListsInProgress.Clear();
            GeBoAPI.Instance.AutoTranslationHelper.UnregisterOnTranslatingCallback(CharaListHandler);
            _charaListHandlerRegistered = false;
        }

        private static bool ShouldHandleCharaListText(Text textComponent)
        {
            if (textComponent == null) return false;
            var charaList = textComponent.GetComponentInParent<CharaList>();
            return charaList != null && CharaListsInProgress.Contains(charaList);
        }

        private static void CharaListHandler(IComponentTranslationContext obj)
        {
            // TODO: migrate to a version of this call
            /*
            if (ComponentTranslationHelpers.TryTranslateFullName(obj,
                ShouldHandleCharaListText,
                ()=>null,

                t => t != null && PointerCallbackActiveTexts.Contains(t),
                () => null, (CharacterSex)MakerAPI.GetMakerSex()))
            {
                obj.IgnoreComponent();
            }
            */
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled ||
                CharaListsInProgress.Count == 0)
            {
                return;
            }

            var textComponent = obj.Component as Text;
            if (textComponent == null) return;
            var charaList = textComponent.GetComponentInParent<CharaList>();
            if (charaList == null || !CharaListsInProgress.Contains(charaList)) return;

            CharacterSex sex;
            if (charaList.name.EndsWith("_Male", StringComparison.OrdinalIgnoreCase))
            {
                sex = CharacterSex.Male;
            }
            else if (charaList.name.EndsWith("_Female", StringComparison.OrdinalIgnoreCase))
            {
                sex = CharacterSex.Female;
            }
            else
            {
                return;
            }

            /*
             var scope = new NameScope(sex);
            if (TranslationHelper.TryFastTranslateFullName(scope, textComponent.text, out var translatedName))
            {
                obj.OverrideTranslatedText(translatedName);
                obj.IgnoreComponent();
            }
            */
            if (ComponentTranslationHelpers.TryTranslateFullName(obj,
                ShouldHandleCharaListText,
                () => null,
                sex))
            {
                obj.IgnoreComponent();
            }
        }
    }
}
