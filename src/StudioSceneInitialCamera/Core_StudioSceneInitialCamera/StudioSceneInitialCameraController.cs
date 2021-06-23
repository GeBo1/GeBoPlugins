using System.Collections;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace StudioSceneInitialCameraPlugin
{
    public class StudioSceneInitialCameraController : SceneCustomFunctionController
    {
        private const string InitialCameraName = "[Initial Camera]";

        internal OCICamera InitialCamera { get; private set; }

        private static ManualLogSource Logger => StudioSceneInitialCamera.Logger;
        public bool InitialCameraReady { get; private set; }
        public bool InitialCameraSavePending { get; private set; }

        private IEnumerator RequestInitialCameraSave()
        {
            Logger.DebugLogDebug($"{nameof(RequestInitialCameraSave)}: start {Time.frameCount}");
            while (!Studio.Studio.IsInstance() || Camera.main == null)
            {
                yield return null;
            }

            /*
            var readyFrame =Time.frameCount + 1;
            while (Time.frameCount < readyFrame)
            {
                yield return null;
            }
            */

            yield return CoroutineUtils.WaitForEndOfFrame;
            if (InitialCamera == null) InitialCameraSavePending = true;
            Logger.DebugLogDebug($"{nameof(RequestInitialCameraSave)}: done {Time.frameCount}");
        }

        protected override void OnSceneLoad(SceneOperationKind operation,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            Logger.DebugLogDebug($"{this.GetPrettyTypeFullName()}.{nameof(OnSceneLoad)}: {operation}");
            if (!StudioSceneInitialCamera.Enabled.Value) return;

            if (operation == SceneOperationKind.Clear)
            {
                RemoveInitialCamera();
                return;
            }


            // don't do anything on import
            if (operation != SceneOperationKind.Load) return;
            StudioSceneInitialCamera.Hooks.EnableChangeCameraHook = false;
            InitialCamera = null;
            InitialCameraReady = false;
            InitialCameraSavePending = false;
            AddInitialCamera();
        }


        internal void SelectInitialCamera()
        {
            Utils.GetCameraControl().SelectInitialCamera();
        }

        private IEnumerator ActivateInitialCameraCoroutine()
        {
            yield return null;
            var updated = false;
            Studio.Studio.Instance.SafeProc(instance =>
            {
                instance.cameraSelector.SafeProc(cs =>
                {
                    cs.SetCamera(null);
                    cs.OnValueChanged(0);
                });

                SelectInitialCamera();
                updated = true;
            });
            if (updated) yield return null;
            InitialCameraReady = true;
        }

        internal void ActivateInitialCamera()
        {
            if (InitialCamera == null || !InitialCameraReady) return;

            InitialCameraReady = false;
            // block keyboard shortcut as well
            StartCoroutine(ActivateInitialCameraCoroutine());
        }

        private void AddInitialCamera()
        {
            if (!StudioAPI.InsideStudio || !StudioAPI.StudioLoaded || !StudioSceneInitialCamera.Enabled.Value) return;
            StartCoroutine(RequestInitialCameraSave());
        }

        private bool TrySaveInitialCameraToButton()
        {
            if (!SceneHasFreeCameraSlots()) return false;
            var result = false;
            Utils.GetSceneInfo().SafeProc(sceneInfo =>
            {
                var i = sceneInfo.cameraData.Length - 1;
                sceneInfo.cameraData[i].Copy(sceneInfo.cameraSaveData);
                result = true;
                Logger.LogInfoMessage($"Saved initial camera to unused Camera Button {i + 1}");
            });

            return result;
        }

        private bool IsInitialCameraSaved()
        {
            var sceneInfo = Utils.GetSceneInfo();
            return sceneInfo != null && sceneInfo.cameraData.Any(c => c.IsInitial());
        }

        private bool SceneHasFreeCameraSlots()
        {
            var sceneInfo = Utils.GetSceneInfo();
            if (sceneInfo == null) return false;

            var cameraData = sceneInfo.cameraData;
            var numFree = 0;
            for (var i = cameraData.Length - 1; i >= 0; i--)
            {
                if (!cameraData[i].IsFree()) return false;
                numFree++;
                if (numFree > 1) break;
            }

            return numFree > 1;
        }

        private static bool AreCameraDataEqual(Studio.CameraControl.CameraData cameraData1,
            Studio.CameraControl.CameraData cameraData2)
        {
            return cameraData1.distance == cameraData2.distance && cameraData1.pos == cameraData2.pos &&
                   cameraData1.rotate == cameraData2.rotate && cameraData1.rotation == cameraData2.rotation;
        }

        public IEnumerator ExecuteInitialCameraSave()
        {
            Logger.DebugLogDebug($"{nameof(ExecuteInitialCameraSave)}: starting {Time.frameCount}");
            if (Studio.Studio.Instance == null ||
                IsInitialCameraSaved() ||
                Utils.GetSceneSaveCamera().IsFree() ||
                TrySaveInitialCameraToButton() ||
                Studio.Studio.Instance.cameraCount == int.MaxValue)
            {
                StudioSceneInitialCamera.Hooks.EnableChangeCameraHook = false;
                Logger.DebugLogDebug(
                    $"{nameof(ExecuteInitialCameraSave)}: no need to add alternate camera, done: {Time.frameCount}");
                yield break;
            }

            Logger.DebugLogDebug($"{nameof(ExecuteInitialCameraSave)}: adding alternate camera {Time.frameCount}");
            // add the camera
            Studio.Studio.Instance.cameraCount++;
            InitialCamera = AddObjectCamera.Add();
            if (InitialCamera == null) yield break;
            InitialCamera.name = InitialCameraName;
            Studio.Studio.Instance.cameraSelector.SafeProc(cs => cs.Init());

            InitialCameraReady = true;


            var changeAmount = InitialCamera.objectInfo.changeAmount;
            var mainCam = Camera.main;
            if (mainCam == null) yield break;
            var mainTransform = mainCam.transform;
            var pos = changeAmount.pos = mainTransform.position;
            var rot = changeAmount.rot = mainTransform.rotation.eulerAngles;
            var scale = changeAmount.scale = mainTransform.localScale;
            changeAmount.OnChange();

            var tno = InitialCamera.treeNodeObject;
            tno.SetVisible(false);
            tno.enableAddChild =
                tno.enableChangeParent = tno.enableCopy = tno.enableDelete = tno.enableVisible = false;

            // other plugins may be moving camera

            for (var i = 0; i < 5; i++)
            {
                yield return null;
                yield return CoroutineUtils.WaitForEndOfFrame;
                if (InitialCamera == null) yield break;
                changeAmount = InitialCamera.objectInfo.changeAmount;
                if (changeAmount.pos == pos && changeAmount.rot == rot && changeAmount.scale == scale) continue;
                changeAmount.pos = pos;
                changeAmount.rot = rot;
                changeAmount.scale = scale;
                changeAmount.OnChange();
            }

            StudioSceneInitialCamera.Hooks.EnableChangeCameraHook = true;
            Logger.DebugLogDebug($"{nameof(ExecuteInitialCameraSave)}: all done {Time.frameCount}");
        }

        private void RemoveInitialCamera()
        {
            InitialCameraSavePending = false;
            if (InitialCamera == null) return;
            InitialCameraReady = false;
            var tmp = InitialCamera;
            InitialCamera = null;
            Studio.Studio.DeleteNode(tmp.treeNodeObject);
        }

        protected override void OnSceneSave()
        {
            RemoveInitialCamera();
        }

        public bool InputKeyProcHandler(Studio.CameraControl instance)
        {
            // handle adding of extra camera (if needed) inside of key handling to avoid
            // issues with changing things out from under other plugins
            if (InitialCameraSavePending)
            {
                InitialCameraSavePending = false;
                StartCoroutine(ExecuteInitialCameraSave());
                return false;
            }

            if (!StudioSceneInitialCamera.SelectInitialCameraShortcut.Value.IsDown()) return false;
            instance.SelectInitialCamera();
            return true;
        }
    }
}
