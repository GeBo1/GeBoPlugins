using System;
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

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.CameraControl), nameof(Studio.CameraControl.InputKeyProc))]
            internal static void CameraControl_InputKeyProc_Postfix(Studio.CameraControl __instance, ref bool __result)
            {
                try
                {
                    if (__result || !Enabled.Value || !SelectInitialCameraShortcut.Value.IsDown()) return;
                    __instance.SelectInitialCamera();
                    __result = true;
                }
                catch (Exception err)
                {
                    Logger.LogException(err, __instance, nameof(CameraControl_InputKeyProc_Postfix));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ChangeCamera), typeof(OCICamera), typeof(bool),
                typeof(bool))]
            internal static void Studio_ChangeCamera_Postfix(OCICamera _ociCamera)
            {
                Logger.LogDebug(nameof(Studio_ChangeCamera_Postfix));
                if (_insideHook || StudioSaveLoadApi.LoadInProgress || _ociCamera == null) return;
                try
                {
                    _insideHook = true;
                    if (!Enabled.Value) return;
                    Logger.LogDebug($"{nameof(Studio_ChangeCamera_Postfix)}: Running");
                    var controller = GetController();
                    if (controller == null || controller.InitialCamera == null || !controller.InitialCameraReady ||
                        _ociCamera != controller.InitialCamera)
                    {
                        return;
                    }

                    Logger.LogDebug($"{nameof(Studio_ChangeCamera_Postfix)}: Initial camera selected");
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
