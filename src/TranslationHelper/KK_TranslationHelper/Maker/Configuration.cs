using System.Collections;
using System.Collections.Generic;
using ChaCustom;
using HarmonyLib;
using KKAPI.Maker;
using TranslationHelperPlugin.Chara;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Maker
{
    internal static partial class Configuration
    {
        private const string CharTop = "CharactorTop";

        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
        }

        private static IEnumerable<KeyValuePair<string, string[]>> GetNameInputFieldInfos()
        {
            yield return new KeyValuePair<string, string[]>("firstname", new[] {CharTop, "InputName", "InpFirstName"});
            yield return new KeyValuePair<string, string[]>("lastname", new[] {CharTop, "InputName", "InpLastName"});
            yield return new KeyValuePair<string, string[]>("nickname",
                new[] {CharTop, "InputNickName", "InpNickName"});
        }

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
