using HarmonyLib;
using KKAPI.MainGame;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            GameAPI.RegisterExtraBehaviour<Controller>(GUID);
        }
    }
}
