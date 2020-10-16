using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AIChara;
using BepInEx.Logging;
using HarmonyLib;
using HS2;
using UnityEngine.UI;

namespace GameWhoIsTherePlugin
{
    internal class Hooks
    {
        private static bool _inMapSelecCursorEnter;
        private static int _inMapSelecCursorEnterIndex;

        private static readonly List<Text> InMapSelecCursorLabels = new List<Text> {null, null};
        private static readonly List<ChaFileControl> InMapSeleChaFileControls = new List<ChaFileControl> {null, null};
        internal static ManualLogSource Logger => GameWhoIsThere.Logger;

        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void MapSelecCursorEnterPrefix(MapSelectUI __instance)
        {
            if (!GameWhoIsThere.Instance.InMyRoom) return;
            _inMapSelecCursorEnter = true;
            GameWhoIsThere.Instance.Reset();
            _inMapSelecCursorEnterIndex = 0;
            var i = 0;
            foreach (var uiName in new[] {"firstCharaThumbnailUI", "secondCharaThumbnailUI"})
            {
                try
                {
                    InMapSelecCursorLabels[i] = Traverse.Create(__instance)?.Field(uiName)
                        ?.Field<Text>("text")?.Value;
                }
                catch
                {
                    InMapSelecCursorLabels[i] = null;
                }

                i++;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance)
        {
            if (!_inMapSelecCursorEnter || __instance == null) return;
            InMapSeleChaFileControls[_inMapSelecCursorEnterIndex] = __instance;
            _inMapSelecCursorEnterIndex++;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void MapSelecCursorEnterPostfix()
        {
            if (!_inMapSelecCursorEnter) return;
            _inMapSelecCursorEnter = false;
            for (var i = 0; i < 2; i++)
            {
                var label = InMapSelecCursorLabels[i];
                var chaFileControl = InMapSeleChaFileControls[i];
                InMapSelecCursorLabels[i] = null;
                InMapSelecCursorLabels[i] = null;
                if (label == null || chaFileControl == null || label.text != "???") continue;
                GameWhoIsThere.Instance.ConfigureDisplay(i, label, chaFileControl);
            }
        }
    }
}
