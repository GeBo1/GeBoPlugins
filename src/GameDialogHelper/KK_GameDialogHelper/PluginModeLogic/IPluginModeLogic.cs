namespace GameDialogHelperPlugin.PluginModeLogic
{
    public interface IPluginModeLogic
    {
        string CorrectHighlight { get; }

        string IncorrectHighlight { get; }

        bool EnabledForHeroine(SaveData.Heroine heroine);

        bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo);

        bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int correctAnswer);

        void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo, bool isCorrect);

        void ApplyHighlightSelection(int answerId, UnityEngine.UI.Text text);

        //void OnCorrectAnswerSelected();

        //void OnIncorrectAnswerSelected();
    }
}
