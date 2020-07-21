using System.Collections.Generic;
using System.Linq;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Maker;
using RootMotion.Demos;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace TranslationHelperPlugin.Translation
{
    internal static partial class Configuration
    {
        internal static Dictionary<string, string> ListInfoNameTranslatedMap = new Dictionary<string, string>();

        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            SceneManager.activeSceneChanged += SceneChanged;
            ExtendedSave.CardBeingSaved += CardBeingSaved;

        }

        private static void CardBeingSaved(ChaFile file)
        {
            var toRemove =
                ListInfoNameTranslatedMap.Keys.Where(k => System.IO.Path.GetFileName(k) == file.charaFileName).ToList();
            foreach (var path in toRemove) ListInfoNameTranslatedMap.Remove(path);
        }

        private static void SceneChanged(Scene arg0, Scene arg1)
        {
            ListInfoNameTranslatedMap.Clear();
        }
    }
}
