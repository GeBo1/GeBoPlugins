using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExtensibleSaveFormat;
using HarmonyLib;
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
                ListInfoNameTranslatedMap.Keys.Where(k => Path.GetFileName(k) == file.charaFileName).ToList();
            foreach (var path in toRemove) ListInfoNameTranslatedMap.Remove(path);
        }

        private static void SceneChanged(Scene arg0, Scene arg1)
        {
            ListInfoNameTranslatedMap.Clear();
        }
    }
}
