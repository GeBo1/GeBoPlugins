using GeBoCommon.Utilities;
using HarmonyLib;

namespace TranslationHelperPlugin.Chara
{
    internal partial class Hooks
    {
        internal static void KK_SetupHooks(Harmony harmony)
        {
            var propGetter = AccessTools.PropertyGetter(typeof(ChaFileParameter), nameof(ChaFileParameter.fullname));
            if (propGetter == null) return;
            var prefix = AccessTools.Method(typeof(Hooks), nameof(ChaFileParameterFullnamePrefix));
            if (prefix == null) return;
            harmony.Patch(propGetter, new HarmonyMethod(prefix));
        }

        private static bool ChaFileParameterFullnamePrefix(ChaFileParameter __instance, ref string __result)
        {
            if (!TranslationHelper.ShowGivenNameFirst) return true;
            __result = StringUtils.JoinStrings(TranslationHelper.SpaceJoiner, __instance.firstname,
                __instance.lastname);
            return false;
        }
    }
}
