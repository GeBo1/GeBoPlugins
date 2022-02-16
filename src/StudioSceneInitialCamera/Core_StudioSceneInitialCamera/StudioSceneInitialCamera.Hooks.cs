using System;
using System.Diagnostics.CodeAnalysis;
using GeBoCommon.Utilities;
using HarmonyLib;
using KKAPI.Studio.SaveLoad;
using Studio;

namespace StudioSceneInitialCameraPlugin
{
    partial class StudioSceneInitialCamera
    {
        internal static class Hooks
        {
            private static bool _insideHook;
            public static bool EnableChangeCameraHook { get; set; }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.InputKeyProc))]
            internal static void CameraControl_InputKeyProc_Postfix(Studio.CameraControl __instance, ref bool __result)
            {
                try
                {
                    if (__result || !Enabled.Value) return;
                    var newResult = __result;
                    GetController().SafeProc(controller => newResult = controller.InputKeyProcHandler(__instance));
                    __result = newResult;
                }
                catch (Exception err)
                {
                    Logger.LogException(err, __instance, nameof(CameraControl_InputKeyProc_Postfix));
                }
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ChangeCamera), typeof(OCICamera), typeof(bool),
                typeof(bool))]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "inherited naming")]
            internal static void Studio_ChangeCamera_Postfix(OCICamera _ociCamera)
            {
                if (!EnableChangeCameraHook || _insideHook || StudioSaveLoadApi.LoadInProgress ||
                    _ociCamera == null)
                {
                    return;
                }

                try
                {
                    _insideHook = true;
                    if (!Enabled.Value) return;
                    var controller = GetController();
                    if (controller == null || controller.InitialCamera == null || !controller.InitialCameraReady ||
                        _ociCamera != controller.InitialCamera)
                    {
                        return;
                    }

                    Logger.DebugLogDebug($"{nameof(Studio_ChangeCamera_Postfix)}: Initial camera selected");
                    controller.ActivateInitialCamera();
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Studio_ChangeCamera_Postfix));
                    _insideHook = false;
                }
                finally
                {
                    _insideHook = false;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Save), typeof(string))]
            internal static void Studio_SceneInfo_Save_Prefix(SceneInfo __instance, out bool __state)
            {
                __state = false;
                try
                {
                    if (!PreserveCameraDuringAutosave || !IsAutosaving) return;
                    Logger.LogInfo(
                        $"{nameof(Studio_SceneInfo_Save_Prefix)}: Autosave detected, preserving initial camera");
                    GetController().SafeProc(c => c.TryRestoreInitialCameraData(__instance));
                    __state = true;
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Studio_ChangeCamera_Postfix));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SceneInfo), nameof(SceneInfo.Save), typeof(string))]
            internal static void Studio_SceneInfo_Save_Postfix(SceneInfo __instance, bool __state)
            {
                try
                {
                    if (!Enabled.Value) return;
                    var controller = GetController();
                    if (controller == null) return;
                    if (__state) // preserving camera
                    {
                        controller.TryRestoreInitialCameraData(__instance);
                    }
                    else
                    {
                        controller.StartCoroutine(controller.UpdateBackupInitialCameraData());
                    }
                }
                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Studio_ChangeCamera_Postfix));
                }
            }
        }
    }
}
