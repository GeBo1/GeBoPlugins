using System;
using ADV;
using AIChara;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using HarmonyLib;
using TranslationHelperPlugin.Chara;
using UnityEngine.UI;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Hooks
    {
        private static bool _advTranslationCallbackRegistered;
        private static ADVScene _advScene;

        public static ChaControl GetCurrentAdvSceneCharacter()
        {
            return ComponentTranslationHelpers.GetCurrentCharacter(_advScene);
        }

        private static void AdvTranslationCallback(IComponentTranslationContext context)
        {
            if (context.OriginalText.IsNullOrEmpty()) return;

            bool IsNameLabel(Text textComponent)
            {
#if AI
                return textComponent.name == "NameLabel";
#elif HS2
                return textComponent.name == "Name";
#else
                return false;
#endif
            }

            ComponentTranslationHelpers.TryTranslateFullName(context, IsNameLabel, GetCurrentAdvSceneCharacter);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ADVScene), nameof(ADVScene.OnEnable))]
        internal static void ADVSceneOnEnablePostfix(ADVScene __instance)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            // ADV scene started
            try
            {
                _advScene = __instance;
                if (_advTranslationCallbackRegistered) return;
                GeBoAPI.Instance.AutoTranslationHelper.RegisterOnTranslatingCallback(AdvTranslationCallback);
                _advTranslationCallbackRegistered = true;
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(ADVSceneOnEnablePostfix));
                _advTranslationCallbackRegistered = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ADVScene), nameof(ADVScene.OnDisable))]
        internal static void ADVSceneOnDisablePostfix()
        {
            // ADV scene exited
            try
            {
                _advScene = null;
                GeBoAPI.Instance.AutoTranslationHelper.UnregisterOnTranslatingCallback(AdvTranslationCallback);
                _advTranslationCallbackRegistered = false;
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(ADVSceneOnDisablePostfix));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TextScenario), nameof(TextScenario.ChangeCurrentChara))]
        internal static void TextScenarioChangeCurrentCharaPostfix(TextScenario __instance, bool __result)
        {
            try
            {
                if (!__result || __instance == null ||
                    !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled)
                {
                    return;
                }

                __instance.currentChara.SafeProc(cc =>
                    cc.chaCtrl.GetTranslationHelperController().SafeProc(tc => tc.TranslateCardNames()));
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(TextScenarioChangeCurrentCharaPostfix));
            }
        }
    }
}
