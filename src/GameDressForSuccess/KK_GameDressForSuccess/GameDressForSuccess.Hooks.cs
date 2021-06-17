using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ADV;
using ADV.Commands.Chara;
using ADV.Commands.Game;
using BepInEx.Logging;
using ChaCustom;
using GeBoCommon.Utilities;
using HarmonyLib;
using Manager;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Text = ADV.Commands.Base.Text;

namespace GameDressForSuccessPlugin
{
    partial class GameDressForSuccess
    {
        internal class Hooks
        {
            private const string TogglePrefix = "tglCoorde";
            private const string AutoToggleName = TogglePrefix + "00";


            internal static ManualLogSource Logger => Instance != null ? Instance.Logger : null;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(MapChange), nameof(MapChange.Do))]
            internal static void StartTravelingHook(MapChange __instance)
            {
                try
                {
                    if (!Enabled.Value || Instance == null) return;
                    __instance.SafeProc(
                        i => i.scenario.SafeProc(
                            s => s.currentHeroine.SafeProc(Instance.TravelingStart)));
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(StartTravelingHook));
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ADV.Commands.Effect.SceneFade), nameof(ADV.Commands.Effect.SceneFade.Do))]
            [HarmonyPatch(typeof(Text), nameof(Text.Do))]
            internal static void StopTravelingHook(CommandBase __instance)
            {
                try
                {
                    if (!Enabled.Value || Instance == null) return;
                    __instance.SafeProc(
                        i => i.scenario.SafeProc(
                            s => s.currentHeroine.SafeProc(Instance.TravelingDone)));
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(StopTravelingHook));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Coordinate), nameof(Coordinate.Do))]
            [SuppressMessage("ReSharper", "InconsistentNaming")]
            internal static void CoordinateDoPostfix(Coordinate __instance, int ___no,
                ChaFileDefine.CoordinateType ___type)
            {
                try
                {
                    Logger.DebugLogDebug(
                        $"{nameof(CoordinateDoPostfix)}: no={___no}, type={___type}, monitoringChange={Instance != null && Instance._monitoringChange}");
                    if (Instance == null || !Enabled.Value || !Instance._monitoringChange) return;

                    __instance.SafeProc(coord => coord.scenario.SafeProcObject(s =>
                        s.commandController.SafeProcObject(cc =>
                        {
                            if (!cc.GetChara(___no).IsNullOrNpc()) Instance.DressPlayer(___type);
                        })));
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(CoordinateDoPostfix));
                }
            }

            #region Right Click Clothing Support

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Toggle), nameof(Toggle.OnPointerClick))]
            internal static void ToggleOnPointerClickPrefix(Toggle __instance,
                ref PointerEventData eventData, out Toggle __state)
            {
                __state = null;
                try
                {
                    Logger.DebugLogDebug(nameof(ToggleOnPointerClickPrefix));
                    if (!Enabled.Value ||
                        eventData.button != PointerEventData.InputButton.Right ||
                        __instance == null || __instance.name.IsNullOrEmpty() ||
                        !__instance.name.StartsWith(TogglePrefix))
                    {
                        return;
                    }

                    // everything after this point will fire the default left-click event
                    eventData.button = PointerEventData.InputButton.Left;


                    if (__instance.name == AutoToggleName) return;

                    var autoEnabled = (CustomBase.IsInstance() && CustomBase.Instance.autoClothesState) ||
                                      (Game.IsInstance() && Game.Instance.Player.changeClothesType < 0);

                    if (!autoEnabled) return;

                    Toggle autoToggle = null;
                    __instance.group.SafeProc(g =>
                        autoToggle = g.ActiveToggles().FirstOrDefault(t => t.name == AutoToggleName));
                    __state = autoToggle;
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(ToggleOnPointerClickPrefix));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Toggle), nameof(Toggle.OnPointerClick))]
            internal static void ToggleOnPointerClickPostfix(Toggle __state)
            {
                try
                {
                    Logger.DebugLogDebug(nameof(ToggleOnPointerClickPostfix));
                    if (__state == null) return;
                    // if we get here, click the automatic button afterwards
                    __state.OnSubmit(null);
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(ToggleOnPointerClickPostfix));
                }
            }

            #endregion
        }
    }
}
