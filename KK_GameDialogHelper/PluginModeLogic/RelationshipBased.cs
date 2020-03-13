using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public class RelationshipBased : IPluginModeLogic

    {
        public string CorrectHighlight => GameDialogHelper.CorrectHighlight.Value;

        public string IncorrectHighlight => string.Empty;

        public bool EnabledForHeroine(SaveData.Heroine heroine)
        {
            return heroine != null && heroine.relation >= (int)GameDialogHelper.MinimumRelationshipLevel.Value;
        }

        public bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo) => EnabledForHeroine(heroine);

        public bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer) => EnabledForQuestion(heroine, dialogInfo) && answer == dialogInfo.CorrectAnswerId;

        public void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo) { }
    }
}
