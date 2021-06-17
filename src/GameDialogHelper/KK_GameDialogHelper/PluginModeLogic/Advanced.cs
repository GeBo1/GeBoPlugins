using System;
using System.Collections.Generic;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using Manager;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameDialogHelperPlugin.PluginModeLogic
{
    public class Advanced : IPluginModeLogic
    {
        private const float MaxPercentChance = 1.25f;
        private const int RelationWeight = 3;
        private const int FavorWeight = 2;
        private const int IntimacyWeight = 1;
        private const float FirstGirlfriendBonusScale = 1.05f;
        private const float InvitationBonus = 0.1f;
        private const float MaxBlendChance = (1f + MaxPercentChance) / 2f;

        private readonly Dictionary<int, float> _lastGuessChance = new Dictionary<int, float>();
        private HeroineQuestionKey _lastGuessKey;
        private static ManualLogSource Logger => GameDialogHelper.Logger;


        public string CorrectHighlight => GameDialogHelper.CorrectHighlight.Value;

        public string IncorrectHighlight => GameDialogHelper.IncorrectHighlight.Value;

        public bool EnabledForHeroine(SaveData.Heroine heroine)
        {
            return heroine != null && heroine.chaCtrl.GetGameDialogHelperController() != null;
        }

        public bool EnabledForQuestion(SaveData.Heroine heroine, DialogInfo dialogInfo)
        {
            return EnabledForHeroine(heroine);
        }

        public bool EnableForAnswer(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer)
        {
            if (!EnabledForQuestion(heroine, dialogInfo)) return false;

            var chance = CalculateChance(heroine, dialogInfo, answer);
            var guess = Random.Range(0f, 1f);

            Logger?.DebugLogDebug(
                $"{heroine} {dialogInfo} {answer}: {chance:P} chance of remembering (guess {guess:P})");


            if (chance <= 0f || guess >= chance) return false;
            RecordGuessChance(heroine, dialogInfo.QuestionId, answer, chance, guess);
            return true;
        }


        public void ProcessDialogAnswered(SaveData.Heroine heroine, DialogInfo dialogInfo, bool isCorrect)
        {
            if (heroine == null || dialogInfo == null) return;
            heroine.Remember(dialogInfo.QuestionId, dialogInfo.SelectedAnswerId, isCorrect);
        }

        public void ApplyHighlightSelection(int answerId, TextMeshProUGUI text)
        {
            if (GameDialogHelper.HighlightMode.Value == HighlightMode.ChangeColor)
            {
                var isCorrect = answerId == GameDialogHelper.CurrentDialog.CorrectAnswerId;
                var destColor =
                    isCorrect ? GameDialogHelper.DefaultCorrectColor : GameDialogHelper.DefaultIncorrectColor;

                var guessChance = GetRecordedGuessChance(GameDialogHelper.TargetHeroine,
                    GameDialogHelper.CurrentDialog.QuestionId,
                    answerId);
                if (guessChance > 0f) text.color = BlendColors(GameDialogHelper.DefaultColor, destColor, guessChance);
                return;
            }

            GameDialogHelper.DefaultApplyHighlightSelection(answerId, text);
        }

        private static float CalculateChance(SaveData.Heroine heroine, DialogInfo dialogInfo, int answer)
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (dialogInfo.QuestionInfo.QuestionType)
            {
                case QuestionType.Likes:
                    // memory + relationship at 23:1 ratio
                    return CalculateChance(heroine, dialogInfo.QuestionId, answer, 23, 1);

                case QuestionType.Personality:
                    // memory + relationship at 1:1 ratio
                    return CalculateChance(heroine, dialogInfo.QuestionId, answer, 1, 1);

                case QuestionType.PhysicalAttributes:
                    // memory + relationship + intelligence at 5:1:3 ratio + intimacy bonus
                    return CalculateChance(heroine, dialogInfo.QuestionId, answer, 5, 1, 3, heroine.intimacy / 1000f);


                case QuestionType.Invitation:
                    // memory + relationship + intelligence at 1:3:5 ratio + bonus (because obvious)
                    return CalculateChance(heroine, dialogInfo.QuestionId, answer, 1, 3, 5, InvitationBonus);
            }

            return 0f;
        }

        private static float CalculateChance(SaveData.Heroine heroine, int question, int answer, int memoryWeight,
            int relationshipWeight = 0, int intelligenceWeight = 0, float bonus = 0f)
        {
            var total = 0f;
            var sum = 0f;
            if (memoryWeight > 0)
            {
                // let memory give you up to 150%
                var memoryPercentage = Mathf.Clamp(heroine.CurrentRecallChance(question, answer), 0f, 1.5f);
                Logger?.DebugLogDebug($"{answer} memory: {memoryPercentage:P} {memoryWeight}");
                total += memoryWeight;
                sum += memoryPercentage * memoryWeight;
            }

            if (relationshipWeight > 0)
            {
                var relationshipMax = 0;
                var relationshipSum = 0f;
                var relationshipPercentage = 0f;

                void AccumRelationship(int value, int max, int weight)
                {
                    var entry = Mathf.Clamp(value / (float)max, 0f, 1f);
                    relationshipMax += weight;
                    relationshipSum += weight * entry;
                }

                // relation could be -1 if you've never met before
                if (heroine.relation >= 0)
                {
                    AccumRelationship(heroine.relation, 2, RelationWeight);
                    AccumRelationship(heroine.favor, 100, FavorWeight);
                    AccumRelationship(heroine.intimacy, 100, IntimacyWeight);
                    relationshipPercentage = relationshipSum / relationshipMax;


                    // first girl bonus
                    if (heroine.isFirstGirlfriend) relationshipPercentage *= FirstGirlfriendBonusScale;

                    relationshipPercentage = Mathf.Clamp(relationshipPercentage, 0f, 1f);
                }

                Logger?.DebugLogDebug(
                    $"{answer} relationship: {relationshipPercentage:P} {relationshipWeight} ({relationshipSum}/{relationshipMax})");
                total += relationshipWeight;
                sum += relationshipPercentage * relationshipWeight;
            }

            if (intelligenceWeight > 0)
            {
                var intelligencePercentage =
                    Mathf.Clamp(Game.Instance.Player.intellect / 100f, 0f, 1f);
                Logger?.DebugLogDebug(
                    $"{answer} intelligence: {intelligencePercentage:P} {intelligenceWeight}");
                total += intelligenceWeight;
                sum += intelligencePercentage * intelligenceWeight;
            }

            var result = Mathf.Clamp((sum / total) + bonus, 0f, MaxPercentChance);
            Logger?.DebugLogDebug($"CalculateChance => {result:P}");
            return result;
        }

        private void RecordGuessChance(SaveData.Heroine heroine, int questionId, int answerId, float chance,
            float guess)
        {
            var key = new HeroineQuestionKey(heroine, questionId);
            if (key != _lastGuessKey)
            {
                _lastGuessChance.Clear();
                _lastGuessKey = key;
            }

            var guessChance = Mathf.Clamp(Mathf.Min(chance, MaxBlendChance) - guess, float.Epsilon, 1f);
            _lastGuessChance[answerId] = guessChance;
        }

        private float GetRecordedGuessChance(SaveData.Heroine heroine, int questionId, int answerId)
        {
            var key = new HeroineQuestionKey(heroine, questionId);
            var result = key == _lastGuessKey && _lastGuessChance.TryGetValue(answerId, out var tmp)
                ? tmp
                : float.Epsilon;
            return result;
        }

        private static Color BlendColors(Color baseColor, Color destColor, float percent)
        {
            var multiplier = Mathf.Clamp(percent, 0f, 1f);

            float BlendComponent(float baseComponent, float destComponent)
            {
                var result = baseComponent + ((destComponent - baseComponent) * multiplier);
                return result;
            }

            return new Color(
                BlendComponent(baseColor.r, destColor.r),
                BlendComponent(baseColor.g, destColor.g),
                BlendComponent(baseColor.b, destColor.b),
                baseColor.a);
        }

        private class HeroineQuestionKey : IEquatable<HeroineQuestionKey>
        {
            private readonly int _heroineId;
            private readonly int _questionId;

            internal HeroineQuestionKey(SaveData.Heroine heroine, int questionId)
            {
                _questionId = questionId;
                _heroineId = heroine.GetHashCode();
            }

            [UsedImplicitly]
            internal HeroineQuestionKey(SaveData.Heroine heroine, DialogInfo dialogInfo) :
                this(heroine, dialogInfo.QuestionId) { }

            public bool Equals(HeroineQuestionKey other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return _heroineId == other._heroineId && _questionId == other._questionId;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((HeroineQuestionKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_heroineId * 397) ^ _questionId;
                }
            }

            public static bool operator ==(HeroineQuestionKey key1, HeroineQuestionKey key2)
            {
                return key1?.Equals(key2) ?? ReferenceEquals(null, key2);
            }

            public static bool operator !=(HeroineQuestionKey key1, HeroineQuestionKey key2)
            {
                return !(key1 == key2);
            }
        }
    }
}
