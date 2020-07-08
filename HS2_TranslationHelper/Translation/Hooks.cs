using System;
using System.Collections.Generic;
using CharaCustom;
using GameLoadCharaFileSystem;
using GeBoCommon.Chara;
using HarmonyLib;
using TranslationHelperPlugin.Chara;

namespace TranslationHelperPlugin.Translation
{
    internal partial class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController), nameof(LobbyCharaSelectInfoScrollController.Init))]
        [HarmonyPatch(typeof(LobbyCharaSelectInfoScrollController1),
            nameof(LobbyCharaSelectInfoScrollController1.Init))]
        internal static void LobbyCharaSelectInfoScrollControllerInitPrefix(List<GameCharaFileInfo> _lst)
        {
            TranslateFileInfos(_lst);
        }
    }
}
