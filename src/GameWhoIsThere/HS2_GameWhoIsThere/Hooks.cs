using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AIChara;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using HarmonyLib;
using HS2;
using UnityEngine.UI;

namespace GameWhoIsTherePlugin
{
    internal class Hooks
    {
        // ReSharper disable once IdentifierTypo
        private static bool _inMapSelecCursorEnter;

        // ReSharper disable once IdentifierTypo
        private static int _inMapSelecCursorEnterIndex;

        // ReSharper disable once IdentifierTypo
        private static readonly List<Text> InMapSelecCursorLabels = new List<Text> {null, null};

        // ReSharper disable once IdentifierTypo
        private static readonly List<ChaFileControl> InMapSeleChaFileControls = new List<ChaFileControl> {null, null};
        internal static ManualLogSource Logger => GameWhoIsThere.Logger;

        // ReSharper disable once UnusedMethodReturnValue.Global
        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapSelectUI), nameof(MapSelectUI.MapSelecCursorEnter))]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "HarmonyPatch")]
        internal static void MapSelecCursorEnterPrefix(MapSelectUI __instance)
        {
            void SetLabel(int i, MapSelectUI.MapSelectThumbnailUI thumbUi)
            {
                InMapSelecCursorLabels[i] = null;
                thumbUi.SafeProc(ui => InMapSelecCursorLabels[i] = ui.text);
            }

            try
            {
                if (!GameWhoIsThere.Instance.InMyRoom) return;
                _inMapSelecCursorEnter = true;
                GameWhoIsThere.Instance.Reset();
                _inMapSelecCursorEnterIndex = 0;
                if (__instance.SafeProc(inst =>
                {
                    SetLabel(0, inst.firstCharaThumbnailUI);
                    SetLabel(1, inst.secondCharaThumbnailUI);
                }))
                {
                    return;
                }

                InMapSelecCursorLabels[0] = null;
                InMapSelecCursorLabels[1] = null;
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(MapSelecCursorEnterPrefix));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        internal static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance)
        {
            try
            {
                if (!_inMapSelecCursorEnter || __instance == null) return;
                InMapSeleChaFileControls[_inMapSelecCursorEnterIndex] = __instance;
                _inMapSelecCursorEnterIndex++;
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(ChaFileControl_LoadCharaFile_Postfix));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectUI), nameof(MapSelectUI.MapSelecCursorEnter))]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "HarmonyPatch")]
        internal static void MapSelecCursorEnterPostfix()
        {
            try
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
                    var idx = i;
                    GameWhoIsThere.Instance.SafeProc(inst => inst.ConfigureDisplay(idx, label, chaFileControl));
                }
            }

            catch (Exception err)
            {
                Logger.LogException(err, nameof(MapSelecCursorEnterPostfix));
            }
        }
    }
}
