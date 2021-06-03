using System;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using Manager;
using MessagePack;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameDialogHelperPlugin.AdvancedLogicMemory
{
    [MessagePackObject]
    public class AnswerMemory : IMessagePackSerializationCallbackReceiver
    {
        private const float DefaultRecallScale = 0.01f;
        private const ulong DefaultTimesAnsweredForFullRecall = 10;

        private static ManualLogSource Logger => GameDialogHelper.Logger;

        private static float _recallScale = DefaultRecallScale;

        private static ulong _timesAnsweredForFullRecall = DefaultTimesAnsweredForFullRecall;

        [Key("answerId")]
        public readonly int AnswerId;

        [IgnoreMember]
        private long _lastUpdated;

        [IgnoreMember]
        private QuestionMemory _parent;

        public AnswerMemory(int answerId, QuestionMemory parent = null, long lastUpdated = -1)
        {
            AnswerId = answerId;
            _parent = parent ?? _parent;
            _lastUpdated = lastUpdated;
        }

        [UsedImplicitly]
        [SerializationConstructor]
        public AnswerMemory(int answerId, long lastUpdated, ulong timesAnswered, float recall) : this(answerId, null,
            lastUpdated)
        {
            TimesAnswered = timesAnswered;
            Recall = recall;
        }

        [Key("timesAnswered")]
        [field: IgnoreMember]
        public ulong TimesAnswered { get; private set; }

        [Key("recall")]
        [field: IgnoreMember]
        public float Recall { get; private set; }

        [Key("lastUpdated")]
        public long LastUpdated
        {
            get => _lastUpdated;
            private set
            {
                _lastUpdated = Math.Max(value, _lastUpdated);
                if (_parent != null && _lastUpdated > _parent.LastUpdated) _parent.LastUpdated = _lastUpdated;
            }
        }

        public void OnBeforeSerialize()
        {
            Sync();
        }

        public void OnAfterDeserialize()
        {
            Sync();
        }


        [UsedImplicitly]
        public static void SetRecallScale(float recallScale)
        {
            _recallScale = recallScale;
        }

        [UsedImplicitly]
        public static void SetTimesAnsweredForFullRecall(ulong timesAnsweredForFullRecall)
        {
            _timesAnsweredForFullRecall = timesAnsweredForFullRecall;
        }

        public void Remember(bool isCorrect)
        {
            SaveData.Player player = null;
            if (Game.IsInstance()) player = Game.Instance.Player;

            if (player == null)
            {
                Logger?.LogError(
                    $"{nameof(Remember)}: unable to record result, player not accessible");
                return;
            }

            TimesAnswered++;

            var intellectPercent = player.intellect / 100f;
            var swing = Mathf.Clamp(intellectPercent / 10f, 0.001f, 0.05f);

            var recallBase = Mathf.Clamp((float)TimesAnswered / _timesAnsweredForFullRecall, 0.01f, 2f);
            var intellectScale = intellectPercent + Random.Range(-swing, swing);

            Logger?.DebugLogDebug(
                $"{nameof(Remember)}: calculating recall amount: recallBase={recallBase}, intellectScale={intellectScale}, RecallScale={_recallScale}");

            // positive reinforcement
            var newRecall = recallBase * intellectScale * (_recallScale * (isCorrect ? 1.1f : 1f));

            Recall += newRecall;
            Logger?.DebugLogDebug(
                $"{nameof(Remember)}: TimesAnswered={TimesAnswered}, Recall={Recall:P} (added {newRecall:000.000000%})");

            LastUpdated = DateTime.UtcNow.Ticks;
        }

        internal void Sync(QuestionMemory parent = null)
        {
            if (parent != null) _parent = parent;
        }

        public override string ToString()
        {
            return $"{nameof(AnswerMemory)}({nameof(AnswerId)}={AnswerId}, {nameof(TimesAnswered)}={TimesAnswered})";
        }

        public void CopyTo(AnswerMemory answerMemory)
        {
            answerMemory.TimesAnswered = TimesAnswered;
            answerMemory._lastUpdated = LastUpdated;
        }
    }
}
