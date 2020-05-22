using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using GeBoCommon.Utilities;

namespace GameDialogHelperPlugin
{
    public class DialogInfo
    {
        private static ManualLogSource Logger => GameDialogHelper.Logger;
        public int SelectedAnswerId { get; private set; }
        public int QuestionId { get; }
        public int CorrectAnswerId { get; }

        public QuestionInfo QuestionInfo { get; }

        public DialogInfo(int questionId, int correctAnswerId, int numAnswers)
        {
            QuestionId = questionId;
            CorrectAnswerId = correctAnswerId;
            SelectedAnswerId = -1;

            QuestionInfo = QuestionInfo.GetById(questionId);
            if (QuestionInfo is null)
            {
                Logger.LogWarning($"Unable to find QuestionInfo for Id={questionId}");
                QuestionInfo = QuestionInfo.Default;
            }
        }

        public bool HasBeenAnswered => SelectedAnswerId != -1;
        public bool WasAnsweredCorrectly => SelectedAnswerId == CorrectAnswerId;

        public void RecordAnswer(int answerId)
        {
            SelectedAnswerId = answerId;
        }
    }
}
