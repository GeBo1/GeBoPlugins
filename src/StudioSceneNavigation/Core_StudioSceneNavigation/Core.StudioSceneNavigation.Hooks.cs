using System;
using GeBoCommon.Utilities;
using HarmonyLib;
using Studio;

namespace StudioSceneNavigationPlugin
{
    partial class StudioSceneNavigation
    {
        internal static class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.InitInfo))]
            internal static void SceneLoadScene_InitInfo_Postfix(SceneLoadScene __instance)
            {
                Logger.DebugLogDebug($"{nameof(SceneLoadScene_InitInfo_Postfix)}: start");
                try
                {
                    Instance.SafeProc(i => i.OnInitInfo(__instance));
                }
                catch (Exception err)
                {
                    Logger.LogException(err, __instance, nameof(SceneLoadScene_InitInfo_Postfix));
                }
                finally
                {
                    Logger.DebugLogDebug($"{nameof(SceneLoadScene_InitInfo_Postfix)}: end");
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(SceneLoadScene), nameof(SceneLoadScene.SetPage))]
            internal static void SceneLoadScene_SetPage_Postfix(SceneLoadScene __instance, int _page)
            {
                Logger.DebugLogDebug($"{nameof(SceneLoadScene_SetPage_Postfix)}: start: {_page}");
                try
                {
                    Instance.SafeProc(i => i.OnSetPage(__instance, _page));
                }
                catch (Exception err)
                {
                    Logger.LogException(err, __instance, nameof(SceneLoadScene_InitInfo_Postfix));
                }
                finally
                {
                    Logger.DebugLogDebug($"{nameof(SceneLoadScene_SetPage_Postfix)}: end: {_page}");
                }
            }
        }
    }
}
