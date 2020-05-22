using ADV.Commands.Base;
using ADV.Commands.Chara;
using ADV.Commands.Game;
using BepInEx.Logging;
using HarmonyLib;

namespace GameDressForSuccessPlugin
{
    partial class GameDressForSuccess
    {
        internal class Hooks
        {
            internal static ManualLogSource Logger => Instance?.Logger;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MapChange), "Do")]
            internal static void StartTravelingHook()
            {
                if (!Enabled.Value) return;
                Instance?.TravelingStart();
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ADV.Commands.Effect.SceneFade), "Do")]
            [HarmonyPatch(typeof(Text), "Do")]
            internal static void StopTravelingHook()
            {
                if (!Enabled.Value) return;
                Instance?.TravelingDone();
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Coordinate), "Do")]
            internal static void CoordinateDoPostfix(Coordinate __instance)
            {
                if (!Enabled.Value) return;

                var type =
                    (ChaFileDefine.CoordinateType?)AccessTools.Field(__instance.GetType(), "type")
                        ?.GetValue(__instance);
                if (type.HasValue)
                {
                    Instance?.DressPlayer(type.Value);
                }
            }

            /*
            [HarmonyPrefix]
            [HarmonyPatch(typeof(CommandBase), "Do")]
            internal static void CommandBaseDoPrefix(CommandBase __instance)
            {
                var variable = AccessTools.Field(__instance.GetType(), "variable")?.GetValue(__instance) as string;
                var value = AccessTools.Field(__instance.GetType(), "value")?.GetValue(__instance) as string;
                Logger?.LogError(
                    $"{__instance}: scenario={__instance.scenario}, variable={variable}, value={value}");
            }
            */
        }
    }
}
