using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GameLoadCharaFileSystem;
using GeBoCommon.Utilities;
using HarmonyLib;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), nameof(LobbyCharaSelectInfoScrollController.Init))]
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController1),
            nameof(LobbyCharaSelectInfoScrollController1.Init))]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
        private static void LobbyCharaSelectInfoScrollControllerInitPrefix(List<GameCharaFileInfo> _lst)
        {
            try
            {
                TranslateFileInfos(_lst);
            }
#pragma warning disable CA1031
            catch (Exception err)
            {
                Logger.LogException(err, nameof(LobbyCharaSelectInfoScrollControllerInitPrefix));
            }
#pragma warning restore CA1031
        }
    }
}
