using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using IllusionStudio = Studio.Studio;

namespace TranslationHelperPlugin.Studio
{
    [SuppressMessage("ReSharper", "PartialTypeWithSinglePart")]
    internal partial class Hooks
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;
        private static readonly CoroutineLimiter TreeNodeLimiter = new CoroutineLimiter(30, nameof(TreeNodeLimiter));

        internal static Harmony SetupHooks()
        {
            return Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TreeNodeCtrl), "RefreshVisibleLoop")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
        internal static void RefreshVisibleLoopPatch(TreeNodeObject _source)
        {
            if (TranslationHelper.Instance == null || !IllusionStudio.IsInstance() ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled ||
                !Singleton<IllusionStudio>.Instance.dicInfo.TryGetValue(_source, out var ctrlInfo) ||
                !(ctrlInfo is OCIChar oChar))
            {
                return;
            }



            void Handler(string translatedName)
            {
                if (string.IsNullOrEmpty(translatedName)) return;
                _source.textName = translatedName;
            }

            oChar.charInfo.SafeProcObject(ci =>
                ci.chaFile.SafeProc(cf =>
                    cf.TranslateFullName(Handler)));

        }

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void CharaList_InitCharaList_Prefix(CharaList __instance, ref object __state)
        {
            __state = null;
            if (AutoTranslator.Default is AutoTranslationPlugin xua)
            {
                Logger.LogDebug("Disable XUA");
                xua.DisableAutoTranslator();
                __state = xua;
            }
        }
        */

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharaList), "InitCharaList")]
        internal static void CharaList_InitCharaList_Postfix(CharaList __instance, ref object __state)
        {
            TranslateDisplayList(__instance);
            var _ = __state;
            /*
            Logger.LogDebug("Enable XUA");
            (__state as AutoTranslationPlugin)?.EnableAutoTranslator();
            __state = null;
            */
        }


        private static bool TryApplyAlternateTranslation(NameScope scope, CharaFileInfo charaFileInfo, string origName)
        {
            return Configuration.AlternateStudioCharaLoaderTranslators.Any(tryTranslate =>
                tryTranslate(scope, charaFileInfo, origName));
        }

        private static IEnumerator TranslateDisplayListEntry(CharaFileInfo entry, NameScope scope,
            Action<string> callback = null)
        {
            var origName = entry.name;

            var wrappedCallback =
                CharaFileInfoTranslationManager.MakeCachingCallbackWrapper(origName, entry, scope, callback);

            

            void UpdateName(CharaFileInfo charaFileInfo, string translatedName)
            {
                charaFileInfo.name = charaFileInfo.node.text = translatedName;
            }

            void HandleResult(CharaFileInfo charaFileInfo, ITranslationResult result)
            {
                if (!result.Succeeded) return;
                charaFileInfo.name = charaFileInfo.node.text = result.TranslatedText;
            }

            if (TryApplyAlternateTranslation(scope, entry, origName))
            {
                wrappedCallback(entry.name);
                yield break;
            }

            if (TranslationHelper.TryFastTranslateFullName(scope, origName, entry.file, out var fastName))
            {
                UpdateName(entry, fastName);
                wrappedCallback(entry.name);
                yield break;
            }

            yield return null;
            void Handler(ITranslationResult result)
            {
                HandleResult(entry, result);
                TryApplyAlternateTranslation(scope, entry, origName);
                wrappedCallback(entry.name);
            }

            TranslationHelper.Instance.StartCoroutine(
                TranslationHelper.CardNameManager.TranslateFullName(origName, scope, Handler));
        }

        private static IEnumerator TranslateDisplayListEntryCoroutine(CharaFileInfo entry, NameScope scope,
            Action callback = null)
        {
            var limitCoroutine = TranslationHelper.Instance.StartCoroutine(TreeNodeLimiter.Start());
            if (limitCoroutine != null) yield return limitCoroutine;
            yield return TranslationHelper.Instance.StartCoroutine(TranslateDisplayListEntry(entry, scope));
            TreeNodeLimiter.EndImmediately();
            callback?.Invoke();
        }

        private static void TranslateDisplayList(CharaList charaList)
        {
            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;
            try
            {

                if (charaList == null || TranslationHelper.Instance == null ||
                    !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled)
                {
                    return;
                }

                var cfiList = Traverse.Create(charaList)?.Field<CharaFileSort>("charaFileSort")?.Value?.cfiList;
                if (cfiList == null || cfiList.Count == 0) return;

                var sex = Traverse.Create(charaList)?.Field<int>("sex")?.Value ?? -1;
                if (sex == -1) return;

                var scope = new NameScope((CharacterSex)sex);

                var jobs = 0;

                void Finished()
                {
                    // ReSharper disable once AccessToModifiedClosure
                    jobs--;
                    if (jobs < 1)
                    {
                        Logger.DebugLogDebug(
                            $"TranslateDisplayList: All jobs done: {Time.realtimeSinceStartup - start:000.0000000000} seconds");
                    }
                }

                foreach (var entry in cfiList)
                {
                    jobs++;
                    TranslationHelper.Instance.StartCoroutine(TranslateDisplayListEntryCoroutine(entry, scope, Finished));
                }

                if (jobs > 0)
                {
                    Logger.DebugLogDebug(
                        $"TranslateDisplayList: All jobs started: {Time.realtimeSinceStartup - start:000.0000000000} seconds");
                }

            }
            finally
            {
                Logger.DebugLogDebug($"TranslateDisplayList: {Time.realtimeSinceStartup - start:000.0000000000}");
            }

        }
    }
}
