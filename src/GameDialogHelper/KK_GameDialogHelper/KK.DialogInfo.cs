﻿using BepInEx.Logging;
using GeBoCommon.Utilities;
using JetBrains.Annotations;

namespace GameDialogHelperPlugin
{
    [PublicAPI]
    public class DialogInfo
    {
        public DialogInfo(int questionId, int correctAnswerId, int numAnswers)
        {
            QuestionId = questionId;
            CorrectAnswerId = correctAnswerId;
            NumAnswers = numAnswers;
            SelectedAnswerId = -1;

            QuestionInfo = QuestionInfo.GetById(questionId);
            if (!(QuestionInfo is null)) return;
            Logger?.LogWarning($"Unable to find QuestionInfo for Id={questionId}");
            QuestionInfo = QuestionInfo.Default;
        }

        private static ManualLogSource Logger => GameDialogHelper.Logger;
        public int SelectedAnswerId { get; private set; }
        public int QuestionId { get; }
        public int CorrectAnswerId { get; }

        public int NumAnswers { get; }

        public QuestionInfo QuestionInfo { get; }

        public bool HasBeenAnswered => SelectedAnswerId != -1;
        public bool WasAnsweredCorrectly => SelectedAnswerId == CorrectAnswerId;

        public void RecordAnswer(int answerId)
        {
            Logger.DebugLogDebug($"{nameof(RecordAnswer)}: {answerId}");
            SelectedAnswerId = answerId;
        }
    }
}
