#if TIMERS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaCustom;
using GeBoCommon.Utilities;
using HarmonyLib;
using UnityEngine;

namespace GeBoCommon
{
    internal static partial class Timers
    {
        internal static partial class Hooks
        {
            private static float _charaListLoadingStartTime;

            [HarmonyPrefix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
            public static void CustomScenePrefix()
            {
                try
                {
                    if (!GeBoAPI.TimersEnabled) return;
                    _charaListLoadingStartTime = Time.realtimeSinceStartup;
                }
                catch (Exception err)
                {
                    Logger.LogException(err, $"{typeof(Hooks).GetPrettyTypeFullName()}.{nameof(CustomScenePrefix)}");
                }

            }

            [HarmonyPostfix, HarmonyPatch(typeof(CustomCharaFile), "Initialize")]
            public static void CustomScenePostfix()
            {
                try
                {
                    if (!GeBoAPI.TimersEnabled) return;
                    LogTimerAtEndOfFrame("MakerCharaListLoadingTimer", _charaListLoadingStartTime);
                }
                catch (Exception err)
                {
                    Logger.LogException(err, $"{typeof(Hooks).GetPrettyTypeFullName()}.{nameof(CustomScenePostfix)}");
                }
            }
        }
    }
}
#endif
