using GameLoadCharaFileSystem;
using HarmonyLib;
using TranslationHelperPlugin.Utils;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Translation

{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            CharaFileInfoWrapper.RegisterWrapperType(typeof(GameCharaFileInfo), typeof(GameCharaFileInfoWrapper));
        }
    }
}
