using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ActionGame.Communication;
using ADV;
using ADV.Commands.Base;
using GeBoCommon.Utilities;
using HarmonyLib;
using TMPro;
using Info = ActionGame.Communication.Info;

namespace GameDialogHelperPlugin
{
    public partial class GameDialogHelper
    {
        internal static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Info), nameof(ActionGame.Communication.Info.CreateSelectADV), typeof(Info.SelectInfo),
                typeof(ChangeValueSelectInfo))]
            [HarmonyPatch(typeof(Info), nameof(ActionGame.Communication.Info.CreateSelectADV), typeof(Info.SelectInfo),
                typeof(int))]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
            internal static void Info_CreateSelectADV_Prefix(Info __instance, Info.SelectInfo _info)
            {
                try
                {
                    if (EnabledForCurrentHeroine())
                    {
                        var id = _info.GetQuestionId();
                        SetCurrentDialog(id, InfoCheckSelectConditions(__instance, _info.conditions),
                            _info.choice.Length);

                        if (CurrentDialog.QuestionInfo.Id == -1)
                        {
                            Logger?.LogDebug(
                                $"Unknown question: {CurrentDialog.QuestionId}: {_info.introduction.text}");
                        }
                    }
                    else
                    {
                        ClearCurrentDialog();
                    }
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Info_CreateSelectADV_Prefix));
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

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Choice), nameof(Choice.Do))]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "HarmonyPatch")]
            internal static void Choice_Do_Postfix(List<Choice.ChoiceData> ___choices)
            {
                try
                {
                    if (CurrentDialogHighlights.Count == 0) return;
                    var answerId = -1;
                    foreach (var choice in ___choices)
                    {
                        answerId++;
                        var text = choice.transform.GetComponentInChildren<TextMeshProUGUI>();
                        if (text == null) continue;
                        ApplyHighlightSelections(answerId, text);
                    }

                    CurrentDialogHighlights.Clear();
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Choice_Do_Postfix));
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(Program.Transfer), nameof(Program.Transfer.Create))]
            internal static void Transfer_Create_Prefix(bool multi, Command command, string[] args)
            {
                try
                {
                    _ = multi;
                    if (args.IsNullOrEmpty()) return;
                    if (CurrentlyEnabled && command == Command.Choice)
                    {
                        SaveHighlightSelections(args);
                    }
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Transfer_Create_Prefix));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(Choice), "ButtonProc")]
            internal static void Choice_ButtonProc_Postfix(int __result)
            {
                try
                {
                    if (CurrentDialog == null) return;
                    CurrentDialog.RecordAnswer(__result);
                    ProcessDialogAnswered(__result == CurrentDialog.CorrectAnswerId);
                    ClearCurrentDialog();
                }

                catch (Exception err)
                {
                    Logger.LogException(err, nameof(Choice_ButtonProc_Postfix));
                }
            }
        }
    }
}
