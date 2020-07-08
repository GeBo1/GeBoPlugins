using System;
using System.Collections.Generic;
using System.Linq;
using Manager;
using UnityEngine;
using Random = System.Random;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public class Advanced : IPluginModeLogic
    {
        public string CorrectHighlight => GameDialogHelper.CorrectHighlight.Value;

        public string IncorrectHighlight => GameDialogHelper.IncorrectHighlight.Value;

        public bool EnabledForHeroine(SaveData.Heroine heroine) => heroine != null && heroine.chaCtrl.GetGameDialogHelperController() != null;

        public bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo) => EnabledForHeroine(heroine);
        private float CalculateChance(SaveData.Heroine heroine, int question, int answer, int memoryWeight, int relationshipWeight=0, int intelligenceWeight=0)
        {
            var percentages = new List<float>();
            int i;
            if (memoryWeight > 0)
            {
                // let memory give you up to 150%
                var memoryPercentage = Mathf.Min(1.5f, heroine.TimesAnswerSelected(question, answer) / 10f);
                GameDialogHelper.Logger.LogDebug($"{answer} memory: {memoryPercentage:P}");


                for (i = 0; i < memoryWeight; i++)
                {
                    percentages.Add(memoryPercentage);
                }
            }

            if (relationshipWeight > 0)
            {
                var relationshipPercentage = (heroine.relation + 1) / 3f;
                GameDialogHelper.Logger.LogDebug($"{answer} relationship: {relationshipPercentage:P}");
                for (i = 0; i < relationshipWeight; i++)
                {
                    percentages.Add(relationshipPercentage);
                }
            }

            if (intelligenceWeight > 0)
            {
                var intelligencePercentage = Singleton<Manager.Game>.Instance.Player.intellect / 100f;
                GameDialogHelper.Logger.LogDebug($"{answer} intelligence: {intelligencePercentage:P}");
                for (i = 0; i < intelligenceWeight; i++)
                {
                    percentages.Add(intelligencePercentage);
                }
            }

            var result = percentages.Count > 0 ? percentages.Sum() / percentages.Count : 0f;
            GameDialogHelper.Logger.LogDebug($"CalculateChance => {result}");
            return result;
        }

        public bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer)
        {
            if (!EnabledForQuestion(heroine, dialogInfo)) return false;
            
            var chance = 0f;
            switch (dialogInfo.QuestionInfo.QuestionType)
            {
                case QuestionType.Likes:
                    // memory + relationship at 50:1 ratio
                    chance = CalculateChance(heroine, dialogInfo.QuestionId, answer, 50, 1);
                    break;
                case QuestionType.Personality:
                    // memory + relationship at 2:3 ratio
                    chance = CalculateChance(heroine, dialogInfo.QuestionId, answer, 2, 3);
                    break;
                case QuestionType.PhysicalAttributes:
                    // memory + intelligence at 1:1 ratio
                    chance = CalculateChance(heroine, dialogInfo.QuestionId, answer, 1, 0, 1);
                    break;
                case QuestionType.Invitation:
                    // relationship + intelligence at 1:2 ratio + 0.5 (because you'd have to be pretty stupid)
                    chance = CalculateChance(heroine, dialogInfo.QuestionId, answer, 0, 1, 1) + 0.33f;
                    break;
            }

            GameDialogHelper.Logger.LogDebug($"{heroine} {dialogInfo} {answer}: {chance:P} chance of remembering");

            return chance > 0f && UnityEngine.Random.Range(0f, 1f) < chance;

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
