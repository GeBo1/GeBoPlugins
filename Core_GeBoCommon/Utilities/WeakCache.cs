using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GeBoCommon.Utilities
{
    public class WeakCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private int nextKey = 0;
        private readonly Dictionary<int, TValue> values = new Dictionary<int, TValue>();

        private readonly Dictionary<WeakReference, int> keyLookup = new Dictionary<WeakReference, int>();

        public ICollection<TKey> Keys => this.Select((e) => e.Key).ToList();

        public ICollection<TValue> Values => this.Select((e) => e.Value).ToList();

        public int Count => Keys.Count;

        public bool IsReadOnly => false;

        private int GetNextKey()
        {
            return nextKey++;
        }

        public TValue this[TKey key]
        {
            get => values[keyLookup[new WeakReference(key)]];
            set
            {
                WeakReference keyRef = new WeakReference(key);
                if (!keyLookup.TryGetValue(keyRef, out int valKey))
                {
                    valKey = GetNextKey();
                    keyLookup[keyRef] = valKey;
                }
                values[valKey] = value;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var live = keyLookup.Where((k) => k.Key.IsAlive);
            return live.Select((k) => new KeyValuePair<TKey, TValue>((TKey)k.Key.Target, values[k.Value])).GetEnumerator();
        }

        public void Add(TKey key, TValue value)
        {
            WeakReference keyRef = new WeakReference(key);
            keyLookup.Add(keyRef, 0); // will throw if add should throw
            int valKey = GetNextKey();
            keyLookup[keyRef] = valKey;
            values[valKey] = value;
        }

        public bool Remove(TKey key)
        {
            WeakReference keyRef = new WeakReference(key);
            if (!keyLookup.TryGetValue(keyRef, out int valKey))
            {
                valKey = -1;
            }
            if (keyLookup.Remove(keyRef))
            {
                values.Remove(valKey);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            WeakReference keyRef = new WeakReference(key);
            if (keyLookup.TryGetValue(keyRef, out int valKey))
            {
                value = values[valKey];
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
            keyLookup.Clear();
            values.Clear();
            nextKey = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key) && this[item.Key].Equals(item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (KeyValuePair<TKey, TValue> entry in this)
            {
                array[i] = entry;
                i++;
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(TKey key)
        {
            WeakReference keyRef = new WeakReference(key);
            return keyLookup.ContainsKey(keyRef);
        }

        public void Cleanup()
        {
            var toRemove = keyLookup.Where((k) => !k.Key.IsAlive);
            foreach (var entry in toRemove)
            {
                values.Remove(entry.Value);
                keyLookup.Remove(entry.Key);
            }
        }
    }
}