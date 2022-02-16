using System.Collections;
using System.Collections.Generic;

namespace GeBoCommon.Utilities
{
    public class NormalizedPathDictionary<TValue> : IDictionary<string, TValue>
    {
        private readonly Dictionary<string, TValue> _dictionary =
            new Dictionary<string, TValue>(PathUtils.NormalizedPathComparer);

        public TValue this[string key]
        {
            get => _dictionary[key];
            set => _dictionary[PathUtils.NormalizePath(key)] = value;
        }

        public ICollection<string> Keys => _dictionary.Keys;

        public ICollection<TValue> Values => _dictionary.Values;

        public int Count => _dictionary.Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, TValue>>)_dictionary).IsReadOnly;

        public void Add(string key, TValue value)
        {
            _dictionary.Add(PathUtils.NormalizePath(key), value);
        }

        public void Add(KeyValuePair<string, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, TValue> item)
        {
            return ((ICollection<KeyValuePair<string, TValue>>)_dictionary).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, TValue>)_dictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, TValue>>)_dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, TValue>>)_dictionary).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<string, TValue> item)
        {
            return ((ICollection<KeyValuePair<string, TValue>>)_dictionary).Remove(item);
        }

        public bool TryGetValue(string key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_dictionary).GetEnumerator();
        }
    }
}
