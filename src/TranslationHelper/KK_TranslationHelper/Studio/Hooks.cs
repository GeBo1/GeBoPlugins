using HarmonyLib;
using Studio;
using TranslationHelperPlugin.Chara;

namespace TranslationHelperPlugin.Studio
{
    internal partial class Hooks
    {
        private static bool _inGenderLoader;


        // KK has separate firstname/lastname fields which allows for more accurate translation
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        private static void InitGenderListPrefix()
        {
            _inGenderLoader = true;
            Configuration.KK_StudioCharaLoaderNames.Clear();
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        private static void InitGenderListPostfix()
        {
            _inGenderLoader = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        private static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance)
        {
            if (!_inGenderLoader || __instance == null ||
                __instance.parameter.fullname.IsNullOrWhiteSpace() ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled ||
                Configuration.KK_StudioCharaLoaderNames.ContainsKey(__instance.parameter.fullname))
            {
                return;
            }

            var origName = __instance.parameter.fullname;
            __instance.TranslateFullName(r =>
            {
                var result = r != origName ? r : null;
                Configuration.KK_StudioCharaLoaderNames[origName] = result;
            });
        }
    }
}
