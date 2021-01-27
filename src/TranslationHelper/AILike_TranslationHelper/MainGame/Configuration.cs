using HarmonyLib;
using KKAPI.Maker;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
#if AI
            AI_GameSpecificSetup(harmony);
#elif HS2
            //HS2_GameSpecificSetup(harmony);
#endif

        }
    }
}
