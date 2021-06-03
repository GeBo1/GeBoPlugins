using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AIChara;
using GameLoadCharaFileSystem;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using HarmonyLib;
using HS2;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.UI;

namespace TranslationHelperPlugin.MainGame
{
    internal partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LobbyParameterUI), nameof(LobbyParameterUI.SetParameter), typeof(GameCharaFileInfo),
            typeof(int), typeof(int))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
        internal static void LobbySetGameCharaFileInfoPrefix(GameCharaFileInfo _info)
        {
            Translation.Hooks.TranslateFileInfo(_info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LobbyParameterUI), nameof(LobbyParameterUI.SetParameter), typeof(GameCharaFileInfo),
            typeof(int), typeof(int))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
        internal static void LobbySetGameCharaFileInfoPostfix(LobbyParameterUI __instance, GameCharaFileInfo _info)
        {
            if (_info == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;

            void Handler(ITranslationResult result)
            {
                if (!result.Succeeded || string.IsNullOrEmpty(result.TranslatedText) || __instance == null) return;
                __instance.SafeProc(inst => inst.txtCharaName.SafeProc(tcn => tcn.text = result.TranslatedText));
            }

            Translation.Hooks.TranslateFileInfo(_info, Handler);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        internal static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance)
        {
            try
            {
                if (!_inMapSelecCursorEnter || __instance == null) return;
                var label = InMapSelecCursorLabels[_inMapSelecCursorEnterIndex];
                InMapSelecCursorLabels[_inMapSelecCursorEnterIndex] = null;
                _inMapSelecCursorEnterIndex++;

                if (label == null || !TranslationHelper.CardNameManager.CardNeedsTranslation(__instance)) return;
                __instance.TranslateFullName(r =>
                {
                    if (string.IsNullOrEmpty(r) || label == null) return;
                    TranslationHelper.Instance.StartCoroutine(UpdateText(label, r));
                });
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(ChaFileControl_LoadCharaFile_Postfix));
            }
#pragma warning restore CA1031
        }


        [HarmonyPrefix]
        // ReSharper disable once StringLiteralTypo
        [HarmonyPatch(typeof(MapSelectUI), nameof(MapSelectUI.MapSelecCursorEnter))]
        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Inherited naming")]
        internal static void MapSelecCursorEnterPrefix(MapSelectUI __instance)
        {
            void SetLabel(int i, MapSelectUI.MapSelectThumbnailUI thumbUi)
            {
                InMapSelecCursorLabels[i] = null;
                thumbUi.SafeProc(ui =>
                {
                    InMapSelecCursorLabels[i] = ui.text;
                    GeBoAPI.Instance.AutoTranslationHelper.IgnoreTextComponent(ui.text);
                });
            }

            try
            {
                if (__instance == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
                _inMapSelecCursorEnter = true;
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
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(MapSelecCursorEnterPrefix));
            }
#pragma warning restore CA1031
        }

        // ReSharper disable once StringLiteralTypo IdentifierTypo
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSelectUI), nameof(MapSelectUI.MapSelecCursorEnter))]
        internal static void MapSelecCursorEnterPostfix()
        {
            _inMapSelecCursorEnter = false;
        }

        [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Inherited naming")]
        private static bool IsInMapSelecCursorEnter()
        {
            return _inMapSelecCursorEnter;
        }


        private static IEnumerator UpdateText(Text label, string value)
        {
            // wait for CursorEnter to finish or it may overwrite
            yield return WaitWhileInMapSelecCursorEnter;
            if (label != null && label.text != "???") label.text = value;
            GeBoAPI.Instance.AutoTranslationHelper.UnignoreTextComponent(label);
        }

        // ReSharper disable IdentifierTypo
        private static bool _inMapSelecCursorEnter;

        private static int _inMapSelecCursorEnterIndex;

        private static readonly List<Text> InMapSelecCursorLabels = new List<Text> {null, null};

        private static readonly IEnumerator WaitWhileInMapSelecCursorEnter = new WaitWhile(IsInMapSelecCursorEnter);
        // ReSharper restore IdentifierTypo
    }
}
