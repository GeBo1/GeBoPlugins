using System;
using System.Collections.Generic;
using BepInEx;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
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
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static void InitGenderListPrefix()
        {
            Translation.Configuration.LoadCharaFileMonitorEnabled = true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        internal static void InitGenderListPostfix()
        {
            Translation.Configuration.LoadCharaFileMonitorEnabled = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void StudioInitCharaListPrefix(CharaList __instance)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            if (CharaListsInProgress.Contains(__instance)) return;
            CharaListsInProgress.Add(__instance);
            EnableCharaListHandler();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void StudioInitCharaListPostfix(CharaList __instance)
        {
            CharaListsInProgress.Remove(__instance);
            if (CharaListsInProgress.Count == 0) DisableCharaListHandler();
        }

        internal static void ResetTranslatingCallbacks()
        {
            DisableCharaListHandler();
        }

        private static void EnableCharaListHandler()
        {
            if (_charaListHandlerRegistered) return;
            Logger.LogFatal("Registering CharaListHandler");
            GeBoAPI.Instance.AutoTranslationHelper.RegisterOnTranslatingCallback(CharaListHandler);
            _charaListHandlerRegistered = true;
        }

        private static void DisableCharaListHandler()
        {
            CharaListsInProgress.Clear();
            Logger.LogFatal("Unregistering CharaListHandler");
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
            // FIXME: migrate to a version of this call
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
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled || CharaListsInProgress.Count == 0) return;
            var start = Time.realtimeSinceStartup;
            var ignore = false;
            var newText = string.Empty;
            try
            {
                var textComponent = obj.Component as Text;
                if (textComponent == null) return;
                newText += $"name={textComponent.name}, text={textComponent.text}";
                var charaList = textComponent.GetComponentInParent<CharaList>();
                if (charaList == null || !CharaListsInProgress.Contains(charaList)) return;

                var sex = CharacterSex.Unspecified;
                if (charaList.name.EndsWith("_Male", StringComparison.OrdinalIgnoreCase))
                {
                    sex = CharacterSex.Male;
                }
                else if (charaList.name.EndsWith("_Female", StringComparison.OrdinalIgnoreCase))
                {
                    sex = CharacterSex.Female;
                }

                if (sex == CharacterSex.Unspecified) return;

                var scope = new NameScope(sex);

                if (TranslationHelper.TryFastTranslateFullName(scope, textComponent.text, out var translatedName))
                {
                    newText += $", newText={translatedName}";
                    obj.OverrideTranslatedText(translatedName);
                    obj.IgnoreComponent();
                    ignore = true;
                }
            }
            finally
            {
                Logger.LogDebug(
                    $"{typeof(Configuration).FullName}.{nameof(CharaListHandler)}: {obj.Component} {newText}, Ignore={ignore} {Time.realtimeSinceStartup - start:0.000000000} ({_charaListHandlerRegistered})");
            }
        }
    }
}
