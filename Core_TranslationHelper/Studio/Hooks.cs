using System;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using Studio;

using IllusionStudio = Studio.Studio;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using TranslationHelperPlugin.Chara;
#if AI || HS2
using AIChara;
#endif

namespace TranslationHelperPlugin.Studio
{
    partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        public static bool TrackLoadCharaFile { get; private set; }

        internal static Harmony SetupHooks()
        {
            return HarmonyWrapper.PatchAll(typeof(Hooks));
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(TreeNodeCtrl), "RefreshVisibleLoop")]
        private static void RefreshVisibleLoopPatch(TreeNodeObject _source)
        {
            if (TranslationHelper.Instance == null || !IllusionStudio.IsInstance() ||
                TranslationHelper.Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly ||
                !Singleton<IllusionStudio>.Instance.dicInfo.TryGetValue(_source, out var ctrlInfo) ||
                !(ctrlInfo is OCIChar oChar)) return;
            var name = oChar.charInfo?.chaFile?.parameter?.fullname;
            if (string.IsNullOrEmpty(name)) return;
            _source.textName = name;
        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        private static void InitGenderListPrefix()
        {
            TranslationHelper.AlternateLoadEventsEnabled = true;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitFemaleList")]
        [HarmonyPatch(typeof(CharaList), "InitMaleList")]
        private static void InitGenderListPostfix() => TranslationHelper.AlternateLoadEventsEnabled = false;
        */

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        private static void CharaList_InitCharaList_Postfix(CharaList __instance)
        {
            TranslateDisplayList(__instance);
        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaFileControl), nameof(ChaFileControl.LoadCharaFile), typeof(string), typeof(byte),
            typeof(bool), typeof(bool))]
        private static void ChaFileControl_LoadCharaFile_Postfix(ChaFileControl __instance)
        {
            if (!TrackLoadCharaFile || __instance == null) return;
            __instance.GetTranslationHelperController().SafeProcObject(c => c.TranslateCardNames());
        }
        */


        private static void TranslateDisplayList(CharaList charaList)
        {
            if (charaList == null || TranslationHelper.Instance == null ||
                TranslationHelper.Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly) return;

            var cfiList = Traverse.Create(charaList)?.Field<CharaFileSort>("charaFileSort")?.Value?.cfiList;
            if (cfiList == null) return;

            var sex = Traverse.Create(charaList)?.Field<int>("sex")?.Value ?? -1;
            if (sex == -1) return;

            void HandleResult(CharaFileInfo charaFileInfo, ITranslationResult result)
            {
                if (!result.Succeeded) return;
                charaFileInfo.node.text = result.TranslatedText;
            }

            foreach (var entry in cfiList)
            {
                void Handler(ITranslationResult result) => HandleResult(entry, result);
                // if name splits cleanly to 2 parts split before translating
                var forceSplit =
                    (entry.name.Split(TranslationHelper.SpaceSplitter, StringSplitOptions.RemoveEmptyEntries).Length ==
                     2);
                TranslationHelper.Instance.StartCoroutine(
                    TranslationHelper.CardNameManager.TranslateCardName(entry.name, new NameScope((CharacterSex)sex),
                        forceSplit, Handler));
            }
        }
    }
}
