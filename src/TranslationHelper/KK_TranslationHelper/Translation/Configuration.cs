using System;
using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio;
using KKAPI.Utilities;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace TranslationHelperPlugin.Translation
{
    internal static partial class Configuration
    {
        internal static Dictionary<string, string> ListInfoNameTranslatedMap =
            TranslationHelper.StringCacheInitializer();
        internal static NameScopeDictionary<Dictionary<string,string>> LoadCharaFileTranslatedMap = new NameScopeDictionary<Dictionary<string, string>>(
            ()=> new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        internal static bool LoadCharaFileMonitorEnabled { get; set; } = false;
        
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            SceneManager.activeSceneChanged += SceneChanged;
            ExtendedSave.CardBeingSaved += CardBeingSaved;
            TranslationHelper.BehaviorChanged += TranslationHelperBehaviorChanged;

            if (KoikatuAPI.IsSteamRelease())
            {
                Party.Hooks.Setup();
            }
            else
            {
                Standard.Hooks.Setup();
            }

        }
        private static void TranslationHelperBehaviorChanged(object sender, EventArgs e)
        {
            ListInfoNameTranslatedMap.Clear();
            LoadCharaFileTranslatedMap.Clear();
            if (TranslationHelper.IsShuttingDown) return;
            TranslationHelper.Instance.StartCoroutine(TranslationHelper.WaitOnCardTranslations()
                .AppendCo(ClearCaches));
        }

        internal static byte GuessSex(string club, string personality)
        {
            return (byte)(club == "帯刀" && string.IsNullOrEmpty(personality) ? 0 : 1);
        }
        private static void CleanTranslatedMaps(ChaFile file)
        {
            var maps = LoadCharaFileTranslatedMap.GetScopes().Select(
                s => LoadCharaFileTranslatedMap[s]).ToList();
            maps.Add(ListInfoNameTranslatedMap);
            foreach (var map in maps)
            {
                var toRemove =
                    map.Keys.Where(k => System.IO.Path.GetFileName(k) == file.charaFileName).ToList();
                foreach (var path in toRemove) map.Remove(path);
            }
        }

        private static void CardBeingSaved(ChaFile file)
        {
            CleanTranslatedMaps(file);
        }

        private static void SceneChanged(Scene arg0, Scene arg1)
        {
            if (!StudioAPI.InsideStudio) ClearCaches();
        }

        private static void ClearCaches()
        {
            ListInfoNameTranslatedMap.Clear();
            LoadCharaFileTranslatedMap.Clear();
        }
    }
}
