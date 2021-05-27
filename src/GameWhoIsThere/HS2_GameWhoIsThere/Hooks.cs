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

        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }


        [HarmonyPrefix]
        // ReSharper disable once StringLiteralTypo
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "HarmonyPatch")]
        internal static void MapSelecCursorEnterPrefix(MapSelectUI __instance)
        {
            try
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
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
                    {
                        InMapSelecCursorLabels[i] = null;
                    }
#pragma warning restore CA1031 // Do not catch general exception types

                    i++;
                }
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(MapSelecCursorEnterPrefix));
            }
#pragma warning restore CA1031
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
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(ChaFileControl_LoadCharaFile_Postfix));
            }
#pragma warning restore CA1031
        }

        [HarmonyPostfix]
        // ReSharper disable once StringLiteralTypo
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
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
                    GameWhoIsThere.Instance.ConfigureDisplay(i, label, chaFileControl);
                }
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(MapSelecCursorEnterPostfix));
            }
#pragma warning restore CA1031
        }
    }
}
