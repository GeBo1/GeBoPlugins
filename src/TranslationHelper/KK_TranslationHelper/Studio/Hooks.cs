using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Studio;

namespace TranslationHelperPlugin.Studio
{
    internal partial class Hooks
    {
        // KK has separate firstname/lastname fields which allows for more accurate translation
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "HarmonyPatch")]
        private static void InitGenderListPrefix()
        {
            Translation.Configuration.LoadCharaFileMonitorEnabled = true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "HarmonyPatch")]
        private static void InitGenderListPostfix()
        {
            Translation.Configuration.LoadCharaFileMonitorEnabled = false;
        }
    }
}
