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
            [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
            [HarmonyPostfix]
            internal static void StudioInitInfoPost(SceneLoadScene __instance)
            {
                _currentSceneFolder = string.Empty;
                ScenePaths = SceneUtils.GetSceneLoaderPaths(__instance);
                _normalizedScenePaths = null;
                if (ScenePaths?.Count > 0)
                {
                    _currentSceneFolder = PathUtils.NormalizePath(Path.GetDirectoryName(ScenePaths[0]));
                }
            }
        }
    }
}
