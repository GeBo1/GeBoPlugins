using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChaCustom;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using HarmonyLib;
using KKAPI.Maker;

namespace TranslationHelperPlugin.Translation
{
    partial class Hooks
    {
        // used in maker, starting new game, editing roster
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.AddList))]
        [HarmonyPatch(typeof(ActionGame.ClassRoomFileListCtrl), nameof(ActionGame.ClassRoomFileListCtrl.AddList))]
        private static void FileListCtrlAddListPrefix(CustomFileListCtrl __instance, int index, ref string name,
            string club, string personality)
        {
            if (TranslationHelper.Instance == null || string.IsNullOrEmpty(club) ||
                TranslationHelper.Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly) return;

            byte sex = (byte)(club == "帯刀" && string.IsNullOrEmpty(personality) ? 0 : 1);

            var origName = name;

            void Handler(ITranslationResult result)
            {
                if (!result.Succeeded) return;
                var lstFileInfo = Traverse.Create(__instance)?.Field<List<CustomFileInfo>>("lstFileInfo")?.Value;
                var entry = lstFileInfo?.FirstOrDefault(x => x.index == index && x.name == origName);
                if (entry == null) return;
                var newName = TranslationHelper.ProcessFullnameString(result.TranslatedText);
                entry.name = newName;
            }

            // if name splits cleanly to 2 parts split before translating
            var forceSplit =
                (name.Split(TranslationHelper.SpaceSplitter, StringSplitOptions.RemoveEmptyEntries).Length == 2);
            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateCardName(name, new NameScope((CharacterSex)sex), forceSplit,
                    Handler));
        }
    }
}
