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
                    var id = _info.GetQuestionId();
                    Logger.LogError($"{_info.row} {_info.introduction.file} {_info.GetQuestionId()}");
                    SetCurrentDialog(_info.GetQuestionId(), InfoCheckSelectConditions(__instance, _info.conditions), _info.choice.Length);

                    if (CurrentDialog.QuestionInfo.Id == -1)
                    {
                        Logger.LogDebug($"Unknown question: {CurrentDialog.QuestionId}: {_info.introduction.text}");
                    }
                }
                else
                {
                    ClearCurrentDialog();
                }
            }

            /*
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", new Type[] { typeof(Info.SelectInfo), typeof(ChangeValueSelectInfo) })]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", new Type[] { typeof(Info.SelectInfo), typeof(int) })]
            internal static void Info_CreateSelectADV_Postfix()
            {
                successIndex = null;
            }
            */

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ADV.Program.Transfer), nameof(ADV.Program.Transfer.Create))]
            internal static void Transfer_Create_Prefix(bool multi, ADV.Command command, ref string[] args)
            {
                _ = multi;
                if (CurrentlyEnabled && command == ADV.Command.Choice)
                {
                    HighlightSelections(ref args);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ADV.Commands.Base.Choice), "ButtonProc")]
            internal static void Choice_ButtonProc_Postfix(int __result)
            {
                Logger.LogError($"Clicked on {__result}");
                if (CurrentDialog == null)
                {
                    return;
                }

                CurrentDialog.RecordAnswer(__result);
                ProcessDialogAnswered();
                ClearCurrentDialog();
            }
        }
    }
}
