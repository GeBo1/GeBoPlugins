using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using HarmonyLib;

namespace TranslationHelperPlugin.Chara
{
    internal partial class Hooks
    {
        internal static void KK_SetupHooks(Harmony harmony)
        {
            var propGetter = AccessTools.PropertyGetter(typeof(ChaFileParameter), "fullname");
            if (propGetter == null) return;
            var prefix = AccessTools.Method(typeof(Hooks), nameof(ChaFileParameterFullnamePrefix));
            if (prefix == null) return;
            harmony.Patch(propGetter, new HarmonyMethod(prefix), null);
        }

        // ReSharper disable once RedundantAssignment
        private static bool ChaFileParameterFullnamePrefix(ChaFileParameter __instance, ref string __result)
        {
            if (!TranslationHelper.KK_GivenNameFirst.Value) return true;
            __result = string.Join(" ", new[] {__instance.firstname, __instance.lastname});
            return false;
        }
    }
}
