using System.Collections.Generic;
using System.Linq;
using System.Text;
using KKAPI;
using KKAPI.Chara;
using ExtensibleSaveFormat;
using UnityEngine;

namespace GameDialogHelperPlugin
{
    public class GameDialogHelperController : CharaCustomFunctionController
    {
        public const int DataVersion = 1;
        internal Dictionary<int, Dictionary<int, int>> DialogMemory { get; private set; }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (currentGameMode != GameMode.MainGame || ChaControl.sex == 0)
            {
                SetExtendedData(null);
                return;
            }
            var pluginData = new PluginData
            {
                version = DataVersion
            };
            pluginData.data.Add(nameof(DialogMemory), DialogMemory);
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            DialogMemory = null;

            if (currentGameMode == GameMode.MainGame && ChaControl.sex != 0)
            {
                var pluginData = GetExtendedData();
                if (pluginData != null)
                {
                    if (pluginData.data.TryGetValue(nameof(DialogMemory), out var val))
                    {
                        DialogMemory = val as Dictionary<int, Dictionary<int, int>>;
                    }
                }
            }
            DialogMemory = DialogMemory ?? new Dictionary<int, Dictionary<int, int>>();
        }

        public void Remember(int question, int answer)
        {
            if (!DialogMemory.TryGetValue(question, out var answers))
            {
                DialogMemory[question] = answers = new Dictionary<int, int>();
            }

            if (!answers.ContainsKey(answer))
            {
                answers[answer] = 0;
            }
            answers[answer]++;
            Dump();
        }

        internal void Dump()
        {
            var questions = DialogMemory.Keys.ToList();
            questions.Sort();
            var output = new StringBuilder();
            foreach (var qid in questions)
            {
                output.Append($"{qid:d,3}: ");
                var answers = DialogMemory[qid].Keys.ToList();
                answers.Sort();
                foreach (var answer in answers)
                {
                    output.Append($"{answer:d}({DialogMemory[qid][answer]:d,4}) ");
                }

                output.Append("\n");

            }

            var dump = output.ToString();
            GameDialogHelper.Logger.LogDebug($"DialogMemory dump:\n{dump}");
        }

        public int TimesAnswerSelected(int question, int answer)
        {
            if (!DialogMemory.TryGetValue(question, out var answers)) return 0;
            return answers.TryGetValue(answer, out var result) ? result : 0;
        }

        public int TimesQuestionAnswered(int question)
        {
            return DialogMemory.TryGetValue(question, out var answers) ? answers.Values.Sum() : 0;
        }

        public bool CanRecallAnswer(int question, int answer)
        {
            return DialogMemory.TryGetValue(question, out var answers) && answers.ContainsKey(answer);
        }

        public bool CanRecallQuestion(int question) => DialogMemory.ContainsKey(question);
    }
}
