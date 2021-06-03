using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using MessagePack;

namespace GameDialogHelperPlugin.AdvancedLogicMemory
{
    [MessagePackObject]
    public class QuestionMemory : IDictionary<int, AnswerMemory>, IMessagePackSerializationCallbackReceiver
    {
        [IgnoreMember]
        private readonly Dictionary<int, AnswerMemory> _answerMap = new Dictionary<int, AnswerMemory>();

        [PublicAPI]
        [Key("answers")]
        public readonly List<AnswerMemory> InternalAnswers = new List<AnswerMemory>();

        [IgnoreMember]
        private long _lastUpdated = -1;

        [IgnoreMember]
        private CharaDialogMemory _parent;

        public QuestionMemory(int questionId, CharaDialogMemory parent = null, long lastUpdated = -1)
        {
            QuestionId = questionId;
            _parent = parent ?? _parent;
            LastUpdated = lastUpdated > 0 ? lastUpdated : DateTime.UtcNow.Ticks;
        }

        [UsedImplicitly]
        [SerializationConstructor]
        public QuestionMemory(int questionId, long lastUpdated, List<AnswerMemory> answers) : this(questionId, null,
            lastUpdated)
        {
            InternalAnswers.Clear();
            InternalAnswers.AddRange(answers);
        }

        [Key("questionId")]
        public int QuestionId { get; }

        [Key("lastUpdated")]
        public long LastUpdated
        {
            get => _lastUpdated;
            internal set
            {
                _lastUpdated = Math.Max(value, _lastUpdated);
                if (_parent != null) _parent.LastUpdated = _lastUpdated;
            }
        }

        [IgnoreMember]
        public ICollection<int> Keys => _answerMap.Keys;

        [IgnoreMember]
        public ICollection<AnswerMemory> Values => _answerMap.Values;

        public void Clear()
        {
            _answerMap.Clear();
        }

        public bool Contains(KeyValuePair<int, AnswerMemory> item)
        {
            return _answerMap.Contains(item);
        }


        [IgnoreMember]
        public int Count => _answerMap.Count;

        [IgnoreMember]
        public bool IsReadOnly => ((ICollection<KeyValuePair<int, AnswerMemory>>)_answerMap).IsReadOnly;

        public bool ContainsKey(int key)
        {
            return _answerMap.ContainsKey(key);
        }

        public void Add(int key, AnswerMemory value)
        {
            _answerMap.Add(key, value);
        }

        public bool Remove(int key)
        {
            return _answerMap.Remove(key);
        }

        public bool TryGetValue(int key, out AnswerMemory value)
        {
            return _answerMap.TryGetValue(key, out value);
        }

        [IgnoreMember]
        public AnswerMemory this[int key]
        {
            get
            {
                if (!_answerMap.TryGetValue(key, out var result))
                {
                    result = _answerMap[key] = new AnswerMemory(key, this);
                }

                return result;
            }

            set
            {
                if (key != value.AnswerId)
                {
                    throw new InvalidOperationException($"key ({key}) and AnswerId ({value.AnswerId}) must be equal");
                }

                _answerMap[key] = value;
            }
        }

        public IEnumerator<KeyValuePair<int, AnswerMemory>> GetEnumerator()
        {
            return _answerMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_answerMap).GetEnumerator();
        }

        public void Add(KeyValuePair<int, AnswerMemory> item)
        {
            ((ICollection<KeyValuePair<int, AnswerMemory>>)_answerMap).Add(item);
        }

        public void CopyTo(KeyValuePair<int, AnswerMemory>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<int, AnswerMemory>>)_answerMap).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<int, AnswerMemory> item)
        {
            return ((ICollection<KeyValuePair<int, AnswerMemory>>)_answerMap).Remove(item);
        }

        public void OnBeforeSerialize()
        {
            Sync();
            InternalAnswers.Clear();
            InternalAnswers.AddRange(_answerMap.Values.Where(a=>a.TimesAnswered > 0));
        }

        [IgnoreMember]
        public ulong TimesAnswered => _answerMap.Values.Select(a => a.TimesAnswered).Sum();

        public void OnAfterDeserialize()
        {
            _answerMap.Clear();
            foreach (var answer in InternalAnswers)
            {
                _answerMap[answer.AnswerId] = answer;
            }

            InternalAnswers.Clear();
            Sync();
        }

        internal void Sync(CharaDialogMemory parent = null)
        {
            if (parent != null) _parent = parent;
            foreach (var answer in _answerMap.Values)
            {
                answer.Sync(this);
            }

            if (_answerMap.Count == 0) return;
            LastUpdated = Math.Max(LastUpdated, _answerMap.Values.Select(q => q.LastUpdated).Max());
        }
    }
}
