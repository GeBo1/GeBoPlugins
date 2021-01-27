using TMPro;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public interface IPluginModeLogic
    {
        bool EnabledForHeroine(SaveData.Heroine heroine);

        bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo);

        bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int correctAnswer);

        string CorrectHighlight { get; }

        string IncorrectHighlight { get; }

        void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo, bool isCorrect);

        void ApplyHighlightSelection(int answerId, TextMeshProUGUI text);
        //void OnCorrectAnswerSelected();

        //void OnIncorrectAnswerSelected();
    }
}
