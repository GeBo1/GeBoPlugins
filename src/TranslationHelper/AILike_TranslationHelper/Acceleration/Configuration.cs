using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using UnityEngine.Assertions;

namespace TranslationHelperPlugin.Acceleration
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);

#if AI
            AI_GameSpecificSetup(harmony);
#endif
        }
    }
}
