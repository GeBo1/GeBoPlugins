using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public interface IPluginModeLogic
    {
        bool EnabledForHeroine(SaveData.Heroine heroine);

        bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo);

        bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int correctAnswer);

        string CorrectHighlight { get; }

        string IncorrectHighlight { get; }

        void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo);

        //void OnCorrectAnswerSelected();

        //void OnIncorrectAnswerSelectec();
    }
}
