using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GeBoCommon.Utilities
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    public class WeakCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private int _nextKey;
        private readonly Dictionary<int, TValue> _values = new Dictionary<int, TValue>();

        private readonly Dictionary<WeakReference, int> _keyLookup = new Dictionary<WeakReference, int>();

        public ICollection<TKey> Keys => this.Select((e) => e.Key).ToList();

        public ICollection<TValue> Values => this.Select((e) => e.Value).ToList();

        public int Count => Keys.Count;

        public bool IsReadOnly => false;

        private int GetNextKey()
        {
            return _nextKey++;
        }

        public TValue this[TKey key]
        {
            get => _values[_keyLookup[new WeakReference(key)]];
            set
            {
                var keyRef = new WeakReference(key);
                if (!_keyLookup.TryGetValue(keyRef, out var valKey))
                {
                    valKey = GetNextKey();
                    _keyLookup[keyRef] = valKey;
                }
                _values[valKey] = value;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var live = _keyLookup.Where((k) => k.Key.IsAlive);
            return live.Select((k) => new KeyValuePair<TKey, TValue>((TKey)k.Key.Target, _values[k.Value])).GetEnumerator();
        }

        public void Add(TKey key, TValue value)
        {
            var keyRef = new WeakReference(key);
            _keyLookup.Add(keyRef, 0); // will throw if add should throw
            var valKey = GetNextKey();
            _keyLookup[keyRef] = valKey;
            _values[valKey] = value;
        }

        public bool Remove(TKey key)
        {
            var keyRef = new WeakReference(key);
            if (!_keyLookup.TryGetValue(keyRef, out var valKey))
            {
                valKey = -1;
            }

            if (!_keyLookup.Remove(keyRef)) return false;
            _values.Remove(valKey);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            var keyRef = new WeakReference(key);
            if (_keyLookup.TryGetValue(keyRef, out var valKey))
            {
                value = _values[valKey];
                return true;
            }
            value = default;
            return false;
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _keyLookup.Clear();
            _values.Clear();
            _nextKey = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key) && this[item.Key].Equals(item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            var i = arrayIndex;
            foreach (var entry in this)
            {
                array[i] = entry;
                i++;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Contains(item) && Remove(item.Key);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(TKey key)
        {
            var keyRef = new WeakReference(key);
            return _keyLookup.ContainsKey(keyRef);
        }

        public void Cleanup()
        {
            var toRemove = _keyLookup.Where((k) => !k.Key.IsAlive);
            foreach (var entry in toRemove)
            {
                _values.Remove(entry.Value);
                _keyLookup.Remove(entry.Key);
            }
        }
    }
}
