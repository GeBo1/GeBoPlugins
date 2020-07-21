using HarmonyLib;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Studio
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
        }
    }
}
