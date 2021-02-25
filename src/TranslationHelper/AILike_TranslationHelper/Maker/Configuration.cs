using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CharaCustom;
using HarmonyLib;
using JetBrains.Annotations;
using KKAPI.Maker;
using TranslationHelperPlugin.Chara;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Maker
{
    internal static partial class Configuration
    {
        /*
        internal static Text MakerTextFullName
        {
            get
            {
                var makerBase = MakerAPI.GetMakerBase();
                if (makerBase == null || makerBase.customCtrl == null) return null;
                return Traverse.Create(makerBase.customCtrl).Field<Text>("textFullName")?.Value;
            }
        }
        */

        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            MakerAPI.MakerExiting += CleanupHandler;
            TranslationHelper.BehaviorChanged += CleanupHandler;
        }

        private static void CleanupHandler(object sender, EventArgs e)
        {
            Hooks.ResetTranslatingCallbacks();
        }

        private static IEnumerable<KeyValuePair<string, string[]>> GetNameInputFieldInfos()
        {
            yield return new KeyValuePair<string, string[]>("fullname", new[] {"O_Chara", "Setting", "InputField"});
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static IEnumerator GameSpecificUpdateUICoroutine([NotNull] Controller controller)
        {
            Assert.IsNotNull(controller);
            var makerBase = MakerAPI.GetMakerBase();
            if (makerBase == null) yield break;
            makerBase.customCtrl.UpdateCharaNameText();
            foreach (var element in makerBase.GetComponentsInChildren<CvsO_Chara>())
            {
                element.SafeProcObject(o => o.UpdateCustomUI());
            }
        }
    }
}
