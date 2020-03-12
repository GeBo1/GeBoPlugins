using ActionGame.Communication;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDialogHelperPlugin
{
    public partial class GameDialogHelper
    {
        internal static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", new Type[] { typeof(Info.SelectInfo), typeof(ChangeValueSelectInfo) })]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", new Type[] { typeof(Info.SelectInfo), typeof(int) })]
            internal static void Info_CreateSelectADV_Prefix(Info __instance, ref Info.SelectInfo _info)
            {
                if (EnabledForCurrentHeroine())
                {
                    GameDialogHelper.successIndex = GameDialogHelper.InfoCheckSelectConditions(__instance, _info.conditions);
                }
                else
                {
                    GameDialogHelper.successIndex = null;
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", new Type[] { typeof(Info.SelectInfo), typeof(ChangeValueSelectInfo) })]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", new Type[] { typeof(Info.SelectInfo), typeof(int) })]
            internal static void Info_CreateSelectADV_Postfix()
            {
                successIndex = null;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ADV.Program.Transfer), nameof(ADV.Program.Transfer.Create))]
            internal static void Transfer_Create_Prefix(bool multi, ADV.Command command, ref string[] args)
            {
                _ = multi;
                if (CurrentlyEnabled && successIndex.HasValue && command == ADV.Command.Choice)
                {
                    HighlightSelection(successIndex.Value + 1, ref args);
                }
            }
        }
    }
}