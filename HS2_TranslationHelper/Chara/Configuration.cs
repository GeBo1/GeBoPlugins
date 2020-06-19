using System.Diagnostics;
using HarmonyLib;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Chara
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
        }
    }
}
