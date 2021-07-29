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
                    if (!Enabled.Value || __result) return;
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
        }
    }
}
