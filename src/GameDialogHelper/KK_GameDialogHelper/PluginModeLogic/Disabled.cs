using TMPro;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public class Disabled : IPluginModeLogic
    {
        public string CorrectHighlight => string.Empty;

        public string IncorrectHighlight => string.Empty;

        public bool EnabledForHeroine(SaveData.Heroine heroine)
        {
            return false;
        }

        public bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo)
        {
            return false;
        }

        public bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer)
        {
            return false;
        }

        public void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo, bool isCorrect) { }

        public void ApplyHighlightSelection(int answerId, UnityEngine.UI.Text text) { }
    }
}
