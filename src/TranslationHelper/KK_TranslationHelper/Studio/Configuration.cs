using System;
using System.Collections;
using GeBoCommon.Chara;
using HarmonyLib;
using Studio;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Studio
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            AlternateStudioCharaLoaderTranslators.Add(TryApplyLoadCharaFileTranslatedMap);
#if FALSE
            // Needs XUA 4.13+
            XUnity.AutoTranslator.Plugin.Core.AutoTranslator.Default
                .RegisterOnTranslatingCallback(TestCallback);
#endif
        }

#if FALSE
        private static void TestCallback(XUnity.AutoTranslator.Plugin.Core.ComponentTranslationContext obj)
        {

            var start = UnityEngine.Time.realtimeSinceStartup;
            var ignore = false;
            try
            {

                var textComponent = obj.Component as UnityEngine.UI.Text;
                if (textComponent == null) return;
                var charaList = textComponent.GetComponentInParent<CharaList>();
                if (charaList == null) return;
                var sex = CharacterSex.Unspecified;

                if (charaList.name.EndsWith("_Male", StringComparison.OrdinalIgnoreCase))
                {
                    sex = CharacterSex.Male;
                }
                else if (charaList.name.EndsWith("_Female", StringComparison.OrdinalIgnoreCase))
                {
                    sex = CharacterSex.Female;
                }

                if (sex != CharacterSex.Unspecified)
                {
                    var scope = new NameScope(sex);
                    if (TranslationHelper.TryFastTranslateFullName(scope, textComponent.text, out var translatedName))
                    {
                        Logger.LogDebug($"TryFastTranslateFullName({scope}, {textComponent.text})");
                        obj.OverrideTranslatedText(translatedName);
                        Logger.LogDebug($"Setting {obj.Component}: {obj.OriginalText} => {translatedName}");
                        return;
                    }
                }

                obj.IgnoreComponent();
                ignore = true;
            }
            finally
            {
                Logger.LogDebug(
                    $"TestCallback: {obj.Component}, Ignore={ignore} {UnityEngine.Time.realtimeSinceStartup - start:0.000000000}");
            }
        }
#endif



        private static bool TryApplyLoadCharaFileTranslatedMap(NameScope sexOnlyScope, CharaFileInfo charaFileInfo, string origName)
        {
            if (!Translation.Configuration.LoadCharaFileTranslatedMap[sexOnlyScope].TryGetValue(origName, out var translatedName) ||
                string.IsNullOrEmpty(translatedName))
            {
                return false;
            }

            charaFileInfo.node.text = charaFileInfo.name = translatedName;
            return true;
        }
    }
}
