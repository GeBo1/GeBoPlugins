using System.Collections;
using System.Collections.Generic;
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
        private const int InitialCameraSaveDelayFrames = 5;
        private const float InitialCameraSaveDelayTime = 1f;

        private readonly List<Studio.CameraControl.CameraData> _initialCameraDataBackup =
            new List<Studio.CameraControl.CameraData>();

        private int _initialCameraDataIndexOffset;
        internal OCICamera InitialCamera { get; private set; }

        private static ManualLogSource Logger => StudioSceneInitialCamera.Logger;
        public bool InitialCameraReady { get; private set; }
        public bool InitialCameraSavePending { get; private set; }

        public Studio.CameraControl.CameraData CurrentInitialCameraDataBackup =>
            _initialCameraDataBackup.Count == 0
                ? null
                : _initialCameraDataBackup[_initialCameraDataBackup.Count - _initialCameraDataIndexOffset - 1];


        private IEnumerator RequestInitialCameraSave()
        {
            Logger.DebugLogDebug(
                $"{nameof(RequestInitialCameraSave)}: start {Time.frameCount}/{Time.realtimeSinceStartup}");
            _initialCameraDataBackup.Clear();
            // TODO: does this mess up timeline scenes?
            TryBackupSaveCameraData();
            while (!Studio.Studio.IsInstance() || Camera.main == null)
            {
                yield return null;
            }

            // give TimeLine some time to do it's thing
            var readyFrame = Time.frameCount + InitialCameraSaveDelayFrames;
            var readyTime = Time.realtimeSinceStartup + InitialCameraSaveDelayTime;
            while (Time.frameCount < readyFrame || Time.realtimeSinceStartup < readyTime)
            {
                yield return null;
            }


            yield return CoroutineUtils.WaitForEndOfFrame;

            while (_initialCameraDataBackup.Count == 0 || !TryBackupSaveCameraData() || !TryBackupCurrentCameraData())
            {
                yield return null;
            }

            if (InitialCamera == null) InitialCameraSavePending = true;
            Logger.DebugLogDebug(
                $"{nameof(RequestInitialCameraSave)}: done {Time.frameCount}/{Time.realtimeSinceStartup}");
        }

        protected override void OnSceneLoad(SceneOperationKind operation,
            ReadOnlyDictionary<int, ObjectCtrlInfo> loadedItems)
        {
            Logger.DebugLogDebug($"{this.GetPrettyTypeFullName()}.{nameof(OnSceneLoad)}: {operation}");

            if (operation != SceneOperationKind.Import)
            {
                StopAllCoroutines();
            }

            if (!StudioSceneInitialCamera.Enabled.Value) return;

            if (operation == SceneOperationKind.Clear)
            {
                ResetBackupState();
                RemoveInitialCamera();
                return;
            }

            // don't do anything on import
            if (operation != SceneOperationKind.Load) return;
            if (!StudioSceneInitialCamera.IsAutosaving) ResetBackupState();
            StudioSceneInitialCamera.Hooks.EnableChangeCameraHook = false;
            InitialCamera = null;
            InitialCameraReady = false;
            InitialCameraSavePending = false;
            AddInitialCamera();
        }

        private void ResetBackupState()
        {
            _initialCameraDataBackup.Clear();
            _initialCameraDataIndexOffset = 0;
        }

        internal void SelectInitialCamera(bool storeInSave = false)
        {
            Utils.GetCameraControl().SelectInitialCamera(storeInSave);
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
            if (!StudioAPI.InsideStudio || !StudioAPI.StudioLoaded || !StudioSceneInitialCamera.SaveInitialCamera)
            {
                return;
            }

            StartCoroutine(RequestInitialCameraSave());
        }

        private bool TrySaveInitialCameraToButton()
        {
            if (CurrentInitialCameraDataBackup == null || !SceneHasFreeCameraSlots()) return false;
            var result = false;
            Utils.GetSceneInfo().SafeProc(sceneInfo =>
            {
                var i = sceneInfo.cameraData.Length - 1;
                sceneInfo.cameraData.SafeProc(i, cd =>
                {
                    cd.Copy(CurrentInitialCameraDataBackup);
                    result = true;
                    Logger.LogInfoMessage($"Saved initial camera to unused Camera Button {i + 1}");
                });
            });

            return result;
        }

        internal IEnumerator UpdateBackupInitialCameraData()
        {
            yield return CoroutineUtils.WaitForEndOfFrame;
            // update to latest initial camera data
            TryBackupSaveCameraData();
            // not all games actually update the in-memory cameraSaveData, so add current camera too
            // duplicates are ignored on stack
            TryBackupCurrentCameraData();
            // reset navigation
            _initialCameraDataIndexOffset = 0;
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

        public IEnumerator ExecuteInitialCameraSave()
        {
            Logger.DebugLogDebug($"{nameof(ExecuteInitialCameraSave)}: starting {Time.frameCount}");
            if (Studio.Studio.Instance == null || !StudioSceneInitialCamera.SaveInitialCamera ||
                IsInitialCameraSaved() ||
                Utils.GetSceneSaveCamera().IsFree() ||
                TrySaveInitialCameraToButton() ||
                !StudioSceneInitialCamera.CreateStudioCameraObject ||
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

            Vector3 pos, rot, scale;

            if (CurrentInitialCameraDataBackup != null)
            {
                pos = changeAmount.pos = CurrentInitialCameraDataBackup.pos;
                rot = changeAmount.rot = CurrentInitialCameraDataBackup.rotate;
                scale = changeAmount.scale = CurrentInitialCameraDataBackup.distance;
            }
            else
            {
                var mainCam = Camera.main;
                if (mainCam == null) yield break;
                var mainTransform = mainCam.transform;
                pos = changeAmount.pos = mainTransform.position;
                rot = changeAmount.rot = mainTransform.rotation.eulerAngles;
                scale = changeAmount.scale = mainTransform.localScale;
            }

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

            if (StudioSceneInitialCamera.SelectPreviousInitialCameraShortcut.Value.IsDown())
            {
                if (_initialCameraDataIndexOffset + 1 < _initialCameraDataBackup.Count)
                {
                    _initialCameraDataIndexOffset++;
                    Logger.LogInfoMessage("Selecting previous initial camera");
                }
                else
                {
                    Logger.LogInfoMessage("Already at least recent initial camera");
                }

                SelectInitialCamera(true);
                return true;
            }

            if (StudioSceneInitialCamera.SelectNextInitialCameraShortcut.Value.IsDown())
            {
                if (_initialCameraDataIndexOffset > 0)
                {
                    _initialCameraDataIndexOffset--;
                    Logger.LogInfoMessage("Selecting next initial camera");
                }
                else
                {
                    Logger.LogInfoMessage("Already at most recent initial camera");
                }

                SelectInitialCamera(true);
                return true;
            }

            if (!StudioSceneInitialCamera.SelectInitialCameraShortcut.Value.IsDown()) return false;
            //Utils.GetSceneInfo().SafeProc(si => TryRestoreInitialCameraData(si));
            SelectInitialCamera();
            return true;
        }

        public bool TryBackupInitialCameraData(Studio.CameraControl.CameraData cameraData)
        {
            if (cameraData == null) return false;
            if (!cameraData.IsSame(_initialCameraDataBackup.LastOrDefault())) _initialCameraDataBackup.Add(cameraData);
            return true;
        }

        public bool TryBackupInitialCameraData(Studio.CameraControl cameraControl)
        {
            return cameraControl != null && TryBackupInitialCameraData(cameraControl.Export());
        }

        public bool TryBackupSaveCameraData()
        {
            var result = false;
            Studio.Studio.Instance.SafeProc(instance =>
                instance.sceneInfo.SafeProc(si => result = TryBackupInitialCameraData(si.cameraSaveData)));
            return result;
        }

        public bool TryBackupCurrentCameraData()
        {
            var result = false;
            Studio.Studio.Instance.SafeProc(instance => result = TryBackupInitialCameraData(instance.cameraCtrl));
            return result;
        }

        public bool TryRestoreInitialCameraData(SceneInfo sceneInfo)
        {
            var currentCameraBackup = CurrentInitialCameraDataBackup;
            if (sceneInfo == null || currentCameraBackup == null) return false;
            sceneInfo.cameraSaveData = currentCameraBackup;
            return true;
        }
    }
}
