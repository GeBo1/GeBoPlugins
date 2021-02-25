using System;
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
            TranslationHelper.BehaviorChanged += CleanupHandler;
        }

        private static void CleanupHandler(object sender, EventArgs e)
        {
            Hooks.ResetTranslatingCallbacks();
        }


        private static bool TryApplyLoadCharaFileTranslatedMap(NameScope sexOnlyScope, CharaFileInfo charaFileInfo,
            string origName, bool fastOnly)
        {
            if (!Translation.Configuration.LoadCharaFileTranslatedMap[sexOnlyScope]
                    .TryGetValue(origName, out var translatedName) ||
                string.IsNullOrEmpty(translatedName))
            {
                return false;
            }

            charaFileInfo.node.text = charaFileInfo.name = translatedName;
            return true;
        }
    }
}
