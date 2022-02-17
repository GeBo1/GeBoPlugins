using System;
using System.Collections.Generic;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;

namespace StudioCharaAnimFixPlugin
{
    partial class StudioCharaAnimFix
    {
        internal static class Hooks
        {
            private static readonly HashSet<OCIChar> ChangeInProgress = new HashSet<OCIChar>();

            [HarmonyPrefix]
            [HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara))]
            internal static void OCIChar_ChangeChara_Prefix(OCIChar __instance)
            {
                try
                {
                    ChangeInProgress.Add(__instance);
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(OCIChar_ChangeChara_Prefix));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara))]
            internal static void OCIChar_ChangeChara_Postfix(OCIChar __instance)
            {
                try
                {
                    ChangeInProgress.Remove(__instance);
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(OCIChar_ChangeChara_Postfix));
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ActiveFK))]
            internal static void OCIChar_ActiveFK_Prefix(OCIChar __instance, OIBoneInfo.BoneGroup _group, bool _active,
                bool _force)
            {
                try
                {
                    if (__instance == null || !ChangeInProgress.Contains(__instance)) return;

                    // NeckLook
                    if (FixNeckLookOnReplaceEnabled && _group == OIBoneInfo.BoneGroup.Neck)
                    {
                        SaveNeckLookData(__instance);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(OCIChar_ActiveFK_Prefix));
                }
            }
        }
    }
}
