using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public class Advanced : IPluginModeLogic
    {
        public string CorrectHighlight => GameDialogHelper.CorrectHighlight.Value;

        public string IncorrectHighlight => GameDialogHelper.IncorrectHighlight.Value;

        public bool EnabledForHeroine(SaveData.Heroine heroine) => heroine != null && heroine.chaCtrl.GetGameDialogHelperControler() != null;

        public bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo)
        {
            return heroine != null && heroine.CanRecallQuestion(dialogInfo.QuestionId);
        }

        public bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer)
        {
            return heroine != null && heroine.CanRecallAnswer(dialogInfo.QuestionId, answer);
        }

        public void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo)
        {
            if (heroine != null && dialogInfo != null)
            {
                heroine.Remember(dialogInfo.QuestionId, dialogInfo.SelectedAnswerId);
            }
        }
    }
}
