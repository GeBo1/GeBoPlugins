using System.Linq;
using ActionGame.Communication;
using HarmonyLib;

namespace GameDialogHelperPlugin
{
    public partial class GameDialogHelper
    {
        internal static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", typeof(Info.SelectInfo), typeof(ChangeValueSelectInfo))]
            [HarmonyPatch(typeof(Info), "CreateSelectADV", typeof(Info.SelectInfo), typeof(int))]
            // ReSharper disable once InconsistentNaming
            //internal static void Info_CreateSelectADV_Prefix(Info __instance, ref Info.SelectInfo _info)
            internal static void Info_CreateSelectADV_Prefix(Info __instance, Info.SelectInfo _info)
            {
                if (EnabledForCurrentHeroine())
                {
                    var id = _info.GetQuestionId();
                    Logger.LogError($"{_info.row} {_info.introduction.file} {id}");
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
            internal static bool Transfer_Create_Prefix(bool multi, ADV.Command command, string[] args)
            {
                _ = multi;
                var myargs = args.Select(a => new string(a.ToCharArray())).ToArray();
                if (CurrentlyEnabled && command == ADV.Command.Choice)
                {
                    HighlightSelections(ref myargs);
                    return new Program.Transfer(new ScenarioData.Param(multi, command, args));
                }

                return true;
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
