using HarmonyLib;
using UnityEngine.Assertions;


namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Configuration
    {
        
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            KKAPI.MainGame.GameAPI.RegisterExtraBehaviour<Controller>(GUID);
        }

        
    }
}
