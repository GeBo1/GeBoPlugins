using System;
using System.IO;
using System.Collections.Generic;
using BepInEx.Harmony;
using HarmonyLib;
using Studio;
using XUnity.AutoTranslator.Plugin.Core;
using ExtensibleSaveFormat;
using GeBoCommon.AutoTranslation;


#if AI
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    partial class TranslationHelper
    {
        internal static partial class Hooks
        {
            internal static void SetupHooks()
            {
                var harmony = HarmonyWrapper.PatchAll(typeof(Hooks));
                NameTranslatorHooks.SetupHooks(harmony);

                if (KKAPI.Studio.StudioAPI.InsideStudio)
                {
                    StudioHooks.SetupHooks(harmony);
                }
            }

            [HarmonyPostfix]
#if KK
            [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(bool), typeof(bool))]
#else
            [HarmonyPatch(typeof(ChaFile), "LoadFile", typeof(BinaryReader), typeof(int), typeof(bool), typeof(bool))]
#endif
            private static void ChaFile_LoadFile(ChaFile __instance, bool __result)
            {
                // handle for situations where ExtendedSave events are disabled
                if (ExtendedSave.LoadEventsEnabled || !__result) return;
                
                Instance?.ExtendedSave_CardBeingLoaded(__instance);

            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Reload))]
            private static void ChaControl_Reload(ChaControl __instance)
            {
                if (RegisterActiveCharacters.Value)
                {
                    Instance.StartCoroutine(Instance.RegisterReplacementsWrapper(__instance?.chaFile));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.Load))]
            private static void ChaControl_Load(ChaControl __instance)
            {
                if (RegisterActiveCharacters.Value)
                {
                    Instance?.StartCoroutine(Instance.RegisterReplacementsWrapper(__instance?.chaFile));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.OnDestroy))]
            private static void ChaControl_OnDestroy(ChaControl __instance)
            {
                Instance?.StartCoroutine(Instance.UnregisterReplacements(__instance?.chaFile));
            }

            internal static class StudioHooks
            {

                internal static void SetupHooks(Harmony harmony)
                {
                    harmony.PatchAll(typeof(StudioHooks));
                }


                [HarmonyPrefix]
                [HarmonyPatch(typeof(StudioHooks), nameof(RefreshVisibleLoopPatch))]
                private static void RefreshVisibleLoopPatch(TreeNodeObject _source)
                {
                    if (Instance == null || !Studio.Studio.IsInstance() ||
                        Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly ||
                        !Singleton<Studio.Studio>.Instance.dicInfo.TryGetValue(_source, out var ctrlInfo) ||
                        !(ctrlInfo is OCIChar oChar)) return;
                    var name = oChar.charInfo?.chaFile?.parameter?.fullname;
                    if (string.IsNullOrEmpty(name)) return;
                    _source.textName = name;
                }

                [HarmonyPostfix]
                [HarmonyPatch(typeof(CharaList), "InitCharaList")]
                private static void CharaList_InitCharaList_Postfix(CharaList __instance)
                {
                    TranslateDisplayList(__instance);
                }

                private static void TranslateDisplayList(CharaList charaList)
                {
                    if (charaList == null || Instance == null ||
                        Instance.CurrentCardLoadTranslationMode < CardLoadTranslationMode.CacheOnly) return;

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
                        Instance.StartCoroutine(CardNameManager.TranslateCardName(entry.name, (byte)sex, Handler));
                    }
                }
            }



            internal static class NameTranslatorHooks
            {
                internal const string HookErrorMsg = "Unable to patch XUnity.AutoTranslator.Plugin.Utilities.TranslationScopeProvider";
                internal static void SetupHooks(Harmony harmony)
                {
                    var hooked = false;
                    try
                    {
                        var assembly = AutoTranslator.Default?.GetType().Assembly;
                        var translationScopeProviderType =
                            assembly?.GetType("XUnity.AutoTranslator.Plugin.Utilities.TranslationScopeProvider", true);
                        if (translationScopeProviderType == null) return;
                        var methodInfo = AccessTools.Method(translationScopeProviderType, "GetScope");

                        if (methodInfo == null) return;
                        var prefixMethodInfo = AccessTools.Method(typeof(NameTranslatorHooks), nameof(GetScopePrefix));
                        if (prefixMethodInfo == null) return;
                        var prefix = new HarmonyMethod(prefixMethodInfo);
                        harmony.Patch(methodInfo, prefix);
                        hooked = true;
                    }
                    finally
                    {
                        if (!hooked) Logger.LogError(HookErrorMsg);
                    }
                }

                private static bool GetScopePrefix(object ui, ref int __result)
                {
                    lock (NameTranslator.LockObject)
                    {
                        if (!NameTranslator.ForceScope || ui != null) return true;
                        NameTranslator.ForceScope = false;
                        __result = TranslationHelper.OverrideNameTranslationScope.Value;
                    }
                    return false;
                }
            }
        }
    }
}
