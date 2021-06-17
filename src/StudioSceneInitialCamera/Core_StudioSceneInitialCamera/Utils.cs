using Studio;

namespace StudioSceneInitialCameraPlugin
{
    internal static class Utils
    {
        internal static Studio.CameraControl GetCameraControl()
        {
            Studio.CameraControl cameraControl = null;
            Studio.Studio.Instance.SafeProc(instance => cameraControl = instance.cameraCtrl);
            return cameraControl;
        }

        internal static SceneInfo GetSceneInfo()
        {
            SceneInfo sceneInfo = null;
            Studio.Studio.Instance.SafeProc(instance => sceneInfo = instance.sceneInfo);
            return sceneInfo;
        }

        internal static Studio.CameraControl.CameraData GetResetCamera()
        {
            Studio.CameraControl.CameraData resetData = null;
            GetCameraControl().SafeProc(cc => resetData = cc.cameraReset);
            return resetData;
        }

        internal static Studio.CameraControl.CameraData GetSceneSaveCamera()
        {
            Studio.CameraControl.CameraData sceneSaveCamera = null;
            GetSceneInfo().SafeProc(si => sceneSaveCamera = si.cameraSaveData);
            return sceneSaveCamera;
        }
    }
}
