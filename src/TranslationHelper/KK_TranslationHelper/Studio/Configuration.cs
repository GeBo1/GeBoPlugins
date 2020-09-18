using System.Collections.Generic;
using HarmonyLib;
using Studio;

namespace TranslationHelperPlugin.Studio
{
    internal static partial class Configuration
    {
        internal static readonly Dictionary<string, string>
            KK_StudioCharaLoaderNames = new Dictionary<string, string>();

        internal static void GameSpecificSetup(Harmony harmony)
        {
            AlternateStudioCharaLoaderTranslators.Add(TryApplyBetterCharaLoaderNames);
        }


        private static bool TryApplyBetterCharaLoaderNames(CharaFileInfo charaFileInfo, string origName)
        {
            if (!KK_StudioCharaLoaderNames.TryGetValue(origName, out var translatedName) ||
                string.IsNullOrEmpty(translatedName))
            {
                return false;
            }

            charaFileInfo.node.text = charaFileInfo.name = translatedName;
            return true;
        }
    }
}
