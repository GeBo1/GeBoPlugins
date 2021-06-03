using System;
using System.IO;
using GeBoCommon.Studio;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;

namespace StudioSceneNavigationPlugin
{
    partial class StudioSceneNavigation
    {
        internal static class Hooks
        {
            [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.InitInfo))]
            [HarmonyPostfix]
            internal static void StudioInitInfoPost(SceneLoadScene __instance)
            {
                try
                {
                    _currentSceneFolder = string.Empty;
                    ScenePaths = SceneUtils.GetSceneLoaderPaths(__instance);
                    _normalizedScenePaths = null;
                    ScenePaths.SafeProc(0,
                        p => _currentSceneFolder = PathUtils.NormalizePath(Path.GetDirectoryName(p)));
                    Instance.SafeProc(i => i.ScrollToLastLoadedScene(__instance));
                }
#pragma warning disable CA1031
                catch (Exception err)
                {
                    Logger.LogException(err, __instance, nameof(StudioInitInfoPost));
                }
#pragma warning restore CA1031
            }
        }
    }
}
