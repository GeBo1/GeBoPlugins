using BepInEx.Logging;

namespace GameDialogHelperPlugin
{
    public class DialogInfo
    {
        private static ManualLogSource Logger => GameDialogHelper.Logger;
        public int SelectedAnswerId { get; private set; }
        public int QuestionId { get; }
        public int CorrectAnswerId { get; }

        public int NumAnswers { get;  }

        public QuestionInfo QuestionInfo { get; }

        public DialogInfo(int questionId, int correctAnswerId, int numAnswers)
        {
            QuestionId = questionId;
            CorrectAnswerId = correctAnswerId;
            NumAnswers = numAnswers;
            SelectedAnswerId = -1;

            QuestionInfo = QuestionInfo.GetById(questionId);
            if (!(QuestionInfo is null)) return;
            Logger.LogWarning($"Unable to find QuestionInfo for Id={questionId}");
            QuestionInfo = QuestionInfo.Default;
        }

        public bool HasBeenAnswered => SelectedAnswerId != -1;
        public bool WasAnsweredCorrectly => SelectedAnswerId == CorrectAnswerId;

        public void RecordAnswer(int answerId)
        {
            SelectedAnswerId = answerId;
        }
    }
}
