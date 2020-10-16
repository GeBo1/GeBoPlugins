using System;
using System.Diagnostics.CodeAnalysis;
using ChaCustom;
using HarmonyLib;
using UnityEngine;

namespace TranslationHelperPlugin.Translation.Party
{
    internal class Hooks
    {
        internal static void Setup()
        {
            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks));


            var patched = false;
            // FileListUI.ThreadFileListCtrl<ChaCustom.CustomFileInfo, ChaCustom.CustomFileInfoComponent>
            var baseType = typeof(CustomFileListCtrl).BaseType;
            var onPointerEnter = AccessTools.Method(baseType, nameof(CustomFileListCtrl.OnPointerEnter));
            var onPointerExit = AccessTools.Method(baseType, nameof(CustomFileListCtrl.OnPointerExit));


            if (onPointerEnter != null && onPointerExit != null)
            {

                var onPointerEnterPostfix = AccessTools.Method(typeof(Hooks), nameof(OnPointerEnterPostfix));
                var onPointerExitPrefix =
                    AccessTools.Method(typeof(Translation.Hooks), nameof(Translation.Hooks.OnPointerExitPrefix));

                if (onPointerEnterPostfix != null && onPointerExitPrefix != null)
                {
                    try
                    {
                        harmony.Patch(onPointerEnter, postfix: new HarmonyMethod(onPointerEnterPostfix));
                        harmony.Patch(onPointerExit, new HarmonyMethod(onPointerExitPrefix));
                        patched = true;
                    }
                    catch (Exception err)
                    {
                        TranslationHelper.Logger.LogError($"{typeof(Hooks).FullName}: {err.Message}");
                    }
                }
            }

            if (!patched)
            {
                TranslationHelper.Logger.LogWarning(
                    $"{typeof(Hooks).FullName}: unable to hook pointer enter/exit, some functionality will be disabled");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomFileListCtrl), "Add", typeof(CustomFileInfo))]
        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "HarmonyPatch")]
        internal static void PartyFileListCtrlAddListPrefix(CustomFileListCtrl __instance, CustomFileInfo info)
        {
            Translation.Hooks.FileListCtrlAddListPrefix(__instance, Utils.CharaFileInfoWrapper.CreateWrapper(info));
        }

        /*
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomFileListCtrl), nameof(CustomFileListCtrl.OnPointerEnter))]
        */
        public static void OnPointerEnterPostfix(CustomFileListCtrl __instance, MonoBehaviour fic)
        {
            if (!TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;
            if (fic == null) return;
            var info = Traverse.Create(fic).Property("info").GetValue();
            if (info == null) return;
            var infoType = Traverse.Create(fic).Property("info").GetValueType();
            Translation.Hooks.OnPointerEnterPostfix(__instance,
                Utils.CharaFileInfoWrapper.CreateWrapper(infoType, info));
        }


    }
}
