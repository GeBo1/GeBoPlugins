using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MessagePack;
using MessagePack.Resolvers;

namespace GameDialogHelperPlugin.AdvancedLogicMemory
{
    [MessagePackObject]
    public class CharaDialogMemory : IDictionary<int, QuestionMemory>,
        IMessagePackSerializationCallbackReceiver
    {
        [IgnoreMember]
        private readonly Dictionary<int, QuestionMemory> _questionMap = new Dictionary<int, QuestionMemory>();

        [Key("questions")]
        public readonly List<QuestionMemory> InternalQuestions = new List<QuestionMemory>();

        [IgnoreMember]
        private long _lastUpdated = -1;

        internal CharaDialogMemory(long lastUpdated = -1)
        {
            LastUpdated = lastUpdated > 0 ? lastUpdated : DateTime.UtcNow.Ticks;
        }

        internal CharaDialogMemory(Guid heroineGuid, int heroineGuidVersion=-1, long lastUpdated = -1) : this(lastUpdated)
        {
            HeroineGuid = heroineGuid;
            HeroineGuidVersion = heroineGuidVersion;
        }

        public CharaDialogMemory() : this(Guid.Empty) { }

        public CharaDialogMemory(SaveData.Heroine heroine) :
            this(heroine.GetHeroineGuid()) { }

        [SerializationConstructor]
        [UsedImplicitly]
        public CharaDialogMemory(Guid heroineGuid, long lastUpdated, List<QuestionMemory> questions,
            Guid saveGuid, int heroineGuidVersion=-1) : this(
            heroineGuid,
            heroineGuidVersion,
            lastUpdated)
        {
            SaveGuid = saveGuid;
            InternalQuestions.Clear();
            InternalQuestions.AddRange(questions);
        }

        [IgnoreMember]
        public Guid SessionGuid { get; internal set; } = Guid.Empty;

        [Key("saveGuid")]
        public Guid SaveGuid { get; internal set; } = Guid.Empty;

        [Key("heroineGuid")]
        public Guid HeroineGuid { get; internal set; } = Guid.Empty;

        [Key("lastUpdated")]
        public long LastUpdated
        {
            get => _lastUpdated;
            internal set => _lastUpdated = Math.Max(value, _lastUpdated);
        }

        [Key("heroineGuidVersion")]
        public int HeroineGuidVersion { get; internal set; } = -1;

        [IgnoreMember]
        public int QuestionsAnswered => _questionMap.Count(q => q.Value.TimesAnswered > 0);


        public bool ContainsKey(int key)
        {
            return _questionMap.ContainsKey(key);
        }

        public void Add(int key, QuestionMemory value)
        {
            _questionMap.Add(key, value);
        }

        public bool Remove(int key)
        {
            return _questionMap.Remove(key);
        }

        public bool TryGetValue(int key, out QuestionMemory value)
        {
            return _questionMap.TryGetValue(key, out value);
        }

        [IgnoreMember]
        public QuestionMemory this[int key]
        {
            get
            {
                if (!_questionMap.TryGetValue(key, out var result))
                {
                    result = _questionMap[key] = new QuestionMemory(key, this);
                }

                return result;
            }

            set
            {
                if (key != value.QuestionId)
                {
                    throw new InvalidOperationException($"key ({key}) and AnswerId ({value.QuestionId}) must be equal");
                }

                _questionMap[key] = value;
            }
        }

        [IgnoreMember]
        public ICollection<int> Keys => _questionMap.Keys;

        [IgnoreMember]
        public ICollection<QuestionMemory> Values => _questionMap.Values;


        public IEnumerator<KeyValuePair<int, QuestionMemory>> GetEnumerator()
        {
            return _questionMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_questionMap).GetEnumerator();
        }

        public void Add(KeyValuePair<int, QuestionMemory> item)
        {
            _questionMap.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _questionMap.Clear();
        }

        public bool Contains(KeyValuePair<int, QuestionMemory> item)
        {
            return _questionMap.Contains(item);
        }


        public bool Remove(KeyValuePair<int, QuestionMemory> item)
        {
            return ((IDictionary<int, QuestionMemory>)_questionMap).Remove(item);
        }

        public void CopyTo(KeyValuePair<int, QuestionMemory>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<int, QuestionMemory>>)_questionMap).CopyTo(array, arrayIndex);
        }

        [IgnoreMember]
        public int Count => _questionMap.Count;

        [IgnoreMember]
        public bool IsReadOnly => ((ICollection<KeyValuePair<int, QuestionMemory>>)_questionMap).IsReadOnly;

        public void OnBeforeSerialize()
        {
            Sync();
            InternalQuestions.Clear();
            InternalQuestions.AddRange(_questionMap.Values.Where(q => q.TimesAnswered > 0));
        }

        public void OnAfterDeserialize()
        {
            _questionMap.Clear();
            foreach (var question in InternalQuestions)
            {
                _questionMap[question.QuestionId] = question;
            }

            InternalQuestions.Clear();
            SessionGuid = GameDialogHelper.Instance.CurrentSessionGuid;
            Sync();
        }

        internal void Sync()
        {
            foreach (var question in _questionMap.Values)
            {
                question.Sync(this);
            }


            if (_questionMap.Count == 0) return;
            LastUpdated = Math.Max(LastUpdated, _questionMap.Values.Select(q => q.LastUpdated).Max());
        }

        public bool IsValidForCurrentSession(string logTag = null)
        {
            bool GuidMatch(string name, Guid currentGuid, Guid memoryGuid, out string errMsg)
            {
                var fmt = "{0}: currently '{1}', got '{2}'";
                errMsg = null;
                if (currentGuid == Guid.Empty) return true;
                if (currentGuid == memoryGuid) return true;
                errMsg = string.Format(fmt, name, currentGuid, memoryGuid);
                return false;
            }

            var invalid = new List<string>();
            if (!GuidMatch(nameof(SaveGuid), GameDialogHelper.Instance.CurrentSaveGuid, SaveGuid, out var msg1))
            {
                // SaveGuid may be empty on older data, let it slide for now
                if (SaveGuid != Guid.Empty) invalid.Add(msg1);
            }

            if (!GuidMatch(nameof(SessionGuid), GameDialogHelper.Instance.CurrentSessionGuid, SessionGuid,
                out var msg2))
            {
                invalid.Add(msg2);
            }

            if (invalid.Count <= 0) return true;
            
            var msgParts = new List<string>
            {
                nameof(IsValidForCurrentSession),
                ": failed validation",
                ": ",
                string.Join(", ", invalid.ToArray())
            };
            if (!logTag.IsNullOrWhiteSpace())
            {
                msgParts.Insert(2, $" ({logTag})");
            }

            GameDialogHelper.Logger.LogWarning(string.Join(string.Empty, msgParts.ToArray()));
            return false;

        }
    }
}
