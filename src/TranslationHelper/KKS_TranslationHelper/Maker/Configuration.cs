using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using TranslationHelperPlugin.Chara;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Maker
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
        }

        private static IEnumerable<KeyValuePair<string, string[]>> GetNameInputFieldInfos()
        {
            // ReSharper disable once StringLiteralTypo - inherited
            const string top = "CharactorTop";
            yield return new KeyValuePair<string, string[]>("firstname", new[] {top, "InputName", "InpFirstName"});
            yield return new KeyValuePair<string, string[]>("lastname", new[] {top, "InputName", "InpLastName"});
            yield return new KeyValuePair<string, string[]>("nickname", new[] {top, "InputNickName", "InpNickName"});
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
        private static IEnumerator GameSpecificUpdateUICoroutine(Controller controller)
        {
            Assert.IsNotNull(controller);
            var makerBase = MakerAPI.GetMakerBase();
            if (makerBase == null) yield break;

            makerBase.GetComponentInChildren<CvsChara>().SafeProcObject(o => o.UpdateCustomUI());
            makerBase.GetComponentInChildren<CvsCharaEx>().SafeProcObject(o => o.UpdateCustomUI());
        }
    }
}
