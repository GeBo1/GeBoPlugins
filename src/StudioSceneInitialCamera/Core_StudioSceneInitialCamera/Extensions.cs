namespace StudioSceneInitialCameraPlugin
{
    public static class Extensions
    {
        public static void SelectInitialCamera(this Studio.CameraControl cameraControl)
        {
            cameraControl.SafeProc(cc => Utils.GetSceneInfo().SafeProc(si => cc.Import(si.cameraSaveData)));
        }

        public static bool IsSame(this Studio.CameraControl.CameraData cameraData1,
            Studio.CameraControl.CameraData cameraData2)
        {
            return cameraData1 != null && cameraData2 != null && cameraData1.distance == cameraData2.distance &&
                   cameraData1.pos == cameraData2.pos &&
                   cameraData1.rotate == cameraData2.rotate && cameraData1.rotation == cameraData2.rotation;
        }

        public static bool IsFree(this Studio.CameraControl.CameraData cameraData)
        {
            return cameraData.IsSame(Utils.GetResetCamera());
        }

        public static bool IsInitial(this Studio.CameraControl.CameraData cameraData)
        {
            return cameraData.IsSame(Utils.GetSceneSaveCamera());
        }
    }
}
