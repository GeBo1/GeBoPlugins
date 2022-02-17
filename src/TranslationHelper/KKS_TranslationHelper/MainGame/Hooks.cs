using System;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using SaveData;
using static SaveData.WorldData;

namespace TranslationHelperPlugin.MainGame
{
    internal partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorldData), nameof(FindCallFileData), typeof(CharaData))]
        private static void WorldDataFindCallFileDataPrefix(CharaData charaData)
        {
            try
            {
                if (charaData == null) return;
                Configuration.StartCoroutine(Configuration.TranslateCharaDataNames(charaData));
            }
            catch (Exception err)
            {
                Logger.LogException(err, nameof(WorldDataFindCallFileDataPrefix));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldData), nameof(FindCallFileData), typeof(int), typeof(int))]
        private static void WorldDataFindCallFileDataPostfix(CallFileData __result)
        {
            try
            {
                if (__result == null) return;
                if (TranslationHelper.TryTranslateName(new NameScope(CharacterSex.Male), __result.name,
                        out var translatedName))
                {
                    __result.name = translatedName;
                }
            }
            catch (Exception err)
            {
                Logger.LogException(err, nameof(WorldDataFindCallFileDataPostfix));
            }
        }
    }
}
