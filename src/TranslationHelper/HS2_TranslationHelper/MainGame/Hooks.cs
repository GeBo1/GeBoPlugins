using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AIChara;
using GameLoadCharaFileSystem;
using GeBoCommon.AutoTranslation;
using HarmonyLib;
using HS2;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.UI;

namespace TranslationHelperPlugin.MainGame
{
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Inherited code uses 'Selec'")]
    internal partial class Hooks
    {
        private static bool _inMapSelecCursorEnter;

        private static int _inMapSelecCursorEnterIndex;

        private static readonly List<Text> InMapSelecCursorLabels = new List<Text> {null, null};

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LobbyParameterUI), nameof(LobbyParameterUI.SetParameter), typeof(GameCharaFileInfo),
            typeof(int), typeof(int))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Hook")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        internal static void LobbySetGameCharaFileInfoPrefix(GameCharaFileInfo _info)
        {
            Translation.Hooks.TranslateFileInfo(_info);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LobbyParameterUI), nameof(LobbyParameterUI.SetParameter), typeof(GameCharaFileInfo),
            typeof(int), typeof(int))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Hook")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        internal static void LobbySetGameCharaFileInfoPostfix(LobbyParameterUI __instance, GameCharaFileInfo _info)
        {
            if (_info == null) return;

            void Handler(ITranslationResult result)
            {
                if (!result.Succeeded || string.IsNullOrEmpty(result.TranslatedText)) return;
                var txtCharaName = Traverse.Create(__instance)?.Field<Text>("txtCharaName")?.Value;
                if (txtCharaName == null) return;
                txtCharaName.text = result.TranslatedText;
            }

            Translation.Hooks.TranslateFileInfo(_info, Handler);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        internal static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance)
        {
            if (!_inMapSelecCursorEnter || __instance == null) return;
            var label = InMapSelecCursorLabels[_inMapSelecCursorEnterIndex];
            InMapSelecCursorLabels[_inMapSelecCursorEnterIndex] = null;
            _inMapSelecCursorEnterIndex++;

            if (label == null || !TranslationHelper.CardNameManager.CardNeedsTranslation(__instance)) return;

            __instance.TranslateFullName(r =>
            {
                if (string.IsNullOrEmpty(r)) return;
                TranslationHelper.Instance.StartCoroutine(UpdateText(label, r));
            });
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        [SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Hook")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Crash protection")]
        internal static void MapSelecCursorEnterPrefix(MapSelectUI __instance)
        {
            _inMapSelecCursorEnter = true;
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
        [HarmonyPatch(typeof(MapSelectUI), "MapSelecCursorEnter")]
        [SuppressMessage("ReSharper", "StringLiteralTypo", Justification = "Hook")]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Hook")]
        internal static void MapSelecCursorEnterPostfix()
        {
            _inMapSelecCursorEnter = false;
        }

        private static IEnumerator UpdateText(Text label, string value)
        {
            // wait for CursorEnter to finish or it may overwrite 
            yield return new WaitUntil(() => !_inMapSelecCursorEnter);
            if (label.text == "???") yield break;
            label.text = value;
        }
    }
}
