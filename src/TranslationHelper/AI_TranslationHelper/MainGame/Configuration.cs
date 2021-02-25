using System;
using System.Collections;
using System.Collections.Generic;
using AIChara;
using AIProject;
using GeBoCommon.Chara;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Configuration
    {
        private static Coroutine _merchantRegistrationCoroutine;
        internal static string MerchantCharaName;

        private static bool MerchantReady()
        {
            return Map.IsInstance() && Map.Instance != null && Map.Instance.Merchant != null &&
                   Map.Instance.Merchant.IsInit;
        }

        internal static void AI_GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            TranslationHelper.BehaviorChanged += AIMainGameBehaviorChanged;
        }

        private static void AIMainGameBehaviorChanged(object sender, EventArgs e)
        {
            MerchantCharaName = null;
            // Merchant character is special in that in has as extra name used that's not
            // stored on the card
            if (_merchantRegistrationCoroutine != null)
            {
                TranslationHelper.Instance.StopCoroutine(_merchantRegistrationCoroutine);
            }

            if (TranslationHelper.RegistrationGameModes.Contains(TranslationHelper.Instance.CurrentGameMode))
            {
                _merchantRegistrationCoroutine =
                    TranslationHelper.Instance.StartCoroutine(RegisterMerchantReplacements()
                        .AppendCo(() => _merchantRegistrationCoroutine = null));
            }
        }

        private static IEnumerator RegisterMerchantReplacements()
        {
            yield return new WaitUntil(MerchantReady);
            ChaControl chaControl = null;
            MerchantActor merchant = null;
            string origCharaName = null;
            Map.Instance.SafeProc(i => i.Merchant.SafeProc(m => merchant = m));
            merchant.SafeProc(m => m.ChaControl.SafeProc(cc =>
            {
                chaControl = cc;
                MerchantCharaName = null;
                origCharaName = merchant.CharaName;
            }));
            if (chaControl == null || !chaControl.TryGetTranslationHelperController(out var controller)) yield break;
            controller.TranslateCardNames();
            var scope = NameScope.DefaultNameScope;
            chaControl.chaFile.SafeProc(cf => scope = new NameScope(cf.GetSex()));
            yield return controller.WaitOnTranslations();


            controller.StartMonitoredCoroutine(CardNameTranslationManager.Instance.TranslateCardName(origCharaName,
                scope, result =>
                {
                    if (!result.Succeeded) return;
                    MerchantCharaName = result.TranslatedText;
                    TranslationHelper.RegistrationManager.RegisterReplacementStrings(
                        new Dictionary<string, string> {{MerchantCharaName, MerchantCharaName}});
                }));
        }
    }
}
