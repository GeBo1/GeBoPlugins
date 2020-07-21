using System.Collections;
using System.Collections.Generic;
using CharaCustom;
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
            yield return new KeyValuePair<string, string[]>("fullname", new[] {"O_Chara", "Setting", "InputField"});
        }

        private static IEnumerator GameSpecificUpdateUICoroutine(Controller controller)
        {
            Assert.IsNotNull(controller);
            var makerBase = MakerAPI.GetMakerBase();
            if (makerBase == null) yield break;
            foreach (var element in makerBase.GetComponentsInChildren<CvsO_Chara>())
            {
                element.SafeProcObject(o => o.UpdateCustomUI());
            }
        }
    }
}
