using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KKAPI;
using KKAPI.Chara;
using Studio;
using ExtensibleSaveFormat;
using Illusion.Game.Extensions;
using NodeCanvas.Tasks.Actions;

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
            if (!DialogMemory.TryGetValue(question, out Dictionary<int, int> answers))
            {
                DialogMemory[question] = answers = new Dictionary<int, int>();
            }

            if (!answers.ContainsKey(answer))
            {
                answers[answer] = 0;
            }
            answers[answer]++;
        }

        public int TimesAnswerSelected(int question, int answer)
        {
            if (DialogMemory.TryGetValue(question, out Dictionary<int, int> answers))
            {
                if (answers.TryGetValue(answer, out int result))
                {
                    return result;
                }
            }
            return 0;
        }

        public int TimesQuestionAnswered(int question)
        {
            if (DialogMemory.TryGetValue(question, out Dictionary<int, int> answers))
            {
                return answers.Values.Sum();
            }
            return 0;
        }

        public bool CanRecallAnswer(int question, int answer)
        {
            if (DialogMemory.TryGetValue(question, out Dictionary<int, int> answers))
            {
                return answers.ContainsKey(answer);
            }
            return false;
        }

        public bool CanRecallQuestion(int question) => DialogMemory.ContainsKey(question);
    }
}
