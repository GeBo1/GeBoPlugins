using HarmonyLib;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Maker
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
        }
    }
}
