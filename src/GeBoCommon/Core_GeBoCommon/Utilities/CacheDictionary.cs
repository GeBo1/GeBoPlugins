#if (HS || PH || KK)
#define NEED_LOCKS
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
#if !NEED_LOCKS
using System.Collections.Concurrent;
#endif


namespace GeBoCommon.Utilities
{
    public partial class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    {
        private readonly bool _allowGetsOutsideLock;
        private bool _disposed;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CacheDictionary{TKey, TValue}" /> class.
        /// </summary>
        /// <param name="allowGetsOutsideLock">
        ///     If set to <c>true</c> locks may be bypassed on reads.
        ///     Should only be used when values are not expected to change.
        /// </param>
        public CacheDictionary(bool allowGetsOutsideLock = false)
        {
            _allowGetsOutsideLock = allowGetsOutsideLock;
        }

        public ReaderWriterLockSlim Lock { get; } = LockUtils.CreateLock(LockRecursionPolicy.NoRecursion);

        public void Add(TKey key, TValue value)
        {
            if (_disposed) return;
            using (Lock.GetDisposableUpgradableReadLock())
            {
                if (_dict.ContainsKey(key))
                {
                    throw new ArgumentException($"{nameof(key)} {key} already exists in {this}");
                }

                this[key] = value;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (_disposed) return;
            this[item.Key] = item.Value;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (_disposed) return false;
            return (_allowGetsOutsideLock && _dict.TryGetValue(item.Key, out var lazyValue) &&
                    item.Value.Equals(lazyValue.Value)) ||
                   (TryGetValue(item.Key, out var value) && item.Value.Equals(value));
        }

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_disposed) return false;
            if (_allowGetsOutsideLock && _dict.TryGetValue(item.Key, out var lazyValue) &&
                !item.Value.Equals(lazyValue.Value))
            {
                return false;
            }

            using (Lock.GetDisposableUpgradableReadLock())
            {
                if (!TryGetValue(item.Key, out var value) || !item.Value.Equals(value)) return false;
                return Remove(item.Key);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "can not be less than 0");
            }

            if (array is null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex > array.Length) throw new ArgumentException("not enough space", nameof(array));
            using (Lock.GetDisposableReadOnlyLock())
            {
                if (Count + arrayIndex > array.Length) throw new ArgumentException("not enough space", nameof(array));
                var i = 0;
                foreach (var entry in this)
                {
                    array[arrayIndex + i] = entry;
                    i++;
                }
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [PublicAPI]
        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            return AddOrUpdate(key, AddWrap, updateValueFactory);

            TValue AddWrap(TKey _)
            {
                return addValue;
            }
        }

        private static Func<TKey, SimpleLazy<TValue>> WrapFactory(Func<TKey, TValue> valueFactory)
        {
            SimpleLazy<TValue> Wrapper(TKey key)
            {
                TValue InnerFactory()
                {
                    return valueFactory(key);
                }

                return new SimpleLazy<TValue>(InnerFactory);
            }

            return Wrapper;
        }

        private static Func<TKey, SimpleLazy<TValue>, SimpleLazy<TValue>> WrapFactory(
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            SimpleLazy<TValue> Wrapper(TKey key, SimpleLazy<TValue> oldValue)
            {
                TValue InnerFactory()
                {
                    return updateValueFactory(key, oldValue.Value);
                }

                return new SimpleLazy<TValue>(InnerFactory);
            }

            return Wrapper;
        }

        private static SimpleLazy<TValue> WrapValue(TValue value)
        {
            TValue SetValue()
            {
                return value;
            }

            return new SimpleLazy<TValue>(SetValue);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Lock.Dispose();
            }

            _dict.Clear();
            _disposed = true;
        }

        ~CacheDictionary()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(false);
        }
    }


#if NEED_LOCKS
    public partial class CacheDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, SimpleLazy<TValue>> _dict = new Dictionary<TKey, SimpleLazy<TValue>>();

        public bool ContainsKey(TKey key)
        {
            if (_disposed) return false;
            if (_allowGetsOutsideLock && _dict.ContainsKey(key)) return true;
            using (Lock.GetDisposableReadOnlyLock())
            {
                return _dict.ContainsKey(key);
            }
        }

        public bool Remove(TKey key)
        {
            if (_disposed) return false;
            using (Lock.GetDisposableUpgradableReadLock())
            {
                if (!_dict.ContainsKey(key)) return false;
                using (Lock.GetDisposableWriteLock())
                {
                    return _dict.Remove(key);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            if (_allowGetsOutsideLock && _dict.TryGetValue(key, out var innerVal1))
            {
                value = innerVal1.Value;
                return true;
            }

            using (Lock.GetDisposableReadOnlyLock())
            {
                if (_dict.TryGetValue(key, out var innerVal2))
                {
                    value = innerVal2.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                if (_allowGetsOutsideLock)
                {
                    try
                    {
                        return _dict[key].Value;
                    }
                    catch
                    {
                        // fall through
                    }
                }

                using (Lock.GetDisposableReadOnlyLock())
                {
                    return _dict[key].Value;
                }
            }
            set
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                using (Lock.GetDisposableWriteLock())
                {
                    _dict[key] = WrapValue(value);
                }
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                using (Lock.GetDisposableReadOnlyLock())
                {
                    return _dict.Keys.ToList();
                }
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                using (Lock.GetDisposableReadOnlyLock())
                {
                    return _dict.Values.Select(v => v.Value).ToList();
                }
            }
        }

        public void Clear()
        {
            if (_disposed) return;
            using (Lock.GetDisposableWriteLock())
            {
                _dict.Clear();
            }
        }

        public int Count
        {
            get
            {
                if (_disposed) return 0;
                using (Lock.GetDisposableReadOnlyLock())
                {
                    return _dict.Count;
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            List<KeyValuePair<TKey, SimpleLazy<TValue>>> entries;
            using (Lock.GetDisposableReadOnlyLock())
            {
                entries = _dict.ToList();
            }

            return entries.Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            if (_allowGetsOutsideLock && _dict.TryGetValue(key, out var result)) return result.Value;
            using (Lock.GetDisposableUpgradableReadLock())
            {
                if (_dict.TryGetValue(key, out var lockedResult)) return lockedResult.Value;
                using (Lock.GetDisposableWriteLock())
                {
                    return (_dict[key] = CallFactory(valueFactory, key)).Value;
                }
            }
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            if (_allowGetsOutsideLock)
            {
                // ignore existing result, handled in standard block below
                if (!_dict.TryGetValue(key, out _))
                {
                    using (Lock.GetDisposableWriteLock())
                    {
                        return (_dict[key] = CallFactory(addValueFactory, key)).Value;
                    }
                }
            }

            using (Lock.GetDisposableUpgradableReadLock())
            {
                if (!_dict.TryGetValue(key, out var result))
                {
                    using (Lock.GetDisposableWriteLock())
                    {
                        return (_dict[key] = CallFactory(addValueFactory, key)).Value;
                    }
                }

                using (Lock.GetDisposableWriteLock())
                {
                    return (_dict[key] = CallFactory(updateValueFactory, key, result)).Value;
                }
            }
        }

        private static SimpleLazy<TValue> CallFactory(Func<TKey, TValue> valueFactory, TKey key)
        {
            return WrapFactory(valueFactory)(key);
        }

        private static SimpleLazy<TValue> CallFactory(Func<TKey, TValue, TValue> updateValueFactory, TKey key,
            SimpleLazy<TValue> oldValue)
        {
            return WrapFactory(updateValueFactory)(key, oldValue);
        }
    }

#else
    public partial class CacheDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, SimpleLazy<TValue>> _dict =
            new ConcurrentDictionary<TKey, SimpleLazy<TValue>>();

        public const bool UsingConcurrent = true;

        public bool Remove(TKey key)
        {
            return !_disposed && _dict.TryRemove(key, out _);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public int Count => _disposed ? 0 : _dict.Count;

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            if (_dict.TryGetValue(key, out var innerVal))
            {
                value = innerVal.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            if (_disposed) return false;
            return _dict.ContainsKey(key);
        }

        public TValue this[TKey key]
        {
            get
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                return _dict[key].Value;
            }
            set
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                _dict[key] = WrapValue(value);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                return _dict.Keys.ToList();
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
                return _dict.Values.Select(v => v.Value).ToList();
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            var entries = _dict.ToList();
            return entries.Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Key, kvp.Value.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            return _dict.GetOrAdd(key, WrapFactory(valueFactory)).Value;
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            if (_disposed) throw new InvalidOperationException($"{this} has been disposed");
            return _dict.AddOrUpdate(key, WrapFactory(addValueFactory), WrapFactory(updateValueFactory)).Value;
        }
    }
#endif
}
