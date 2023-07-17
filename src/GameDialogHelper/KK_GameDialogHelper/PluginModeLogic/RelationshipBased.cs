using TMPro;

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

        public bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo)
        {
            return EnabledForHeroine(heroine);
        }

        public bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer)
        {
            return EnabledForQuestion(heroine, dialogInfo) && answer == dialogInfo.CorrectAnswerId;
        }

        public void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo, bool isCorrect) { }

        public void ApplyHighlightSelection(int answerId, UnityEngine.UI.Text text)
        {
            GameDialogHelper.DefaultApplyHighlightSelection(answerId, text);
        }
    }
}
