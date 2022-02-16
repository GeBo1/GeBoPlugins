using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GeBoCommon.Utilities
{
    /// <summary>
    ///     List like class used primarily from passing between threads
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TransferList<T>
    {
        private readonly List<T> _list = new List<T>();

        private readonly object _lock = new object();

        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        public IList<T> Collect(int maxItems = -1)
        {
            if (maxItems == 0 || maxItems < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), maxItems, "must be -1 or greater than 0");
            }

            var result = maxItems > 0 ? new List<T>(maxItems) : new List<T>();
            lock (_lock)
            {
                if (_list == null) return result;
                var items = _list.AsEnumerable();
                if (maxItems > 0) items = items.Take(maxItems);

                result.AddRange(items);

                if (maxItems > 0 && result.Count < _list.Count)
                {
                    _list.RemoveRange(0, result.Count);
                }
                else
                {
                    _list.Clear();
                }
            }

            return result;
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public bool TryCollect(out IList<T> collected, int maxItems = -1)
        {
            collected = null;
            if (_list == null || _list.Count == 0) return false;
            lock (_lock)
            {
                if (IsEmpty()) return false;
                collected = Collect(maxItems);
                return true;
            }
        }

        public bool IsEmpty()
        {
            lock (_lock)
            {
                return _list == null || _list.Count == 0;
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public bool Remove(T item)
        {
            if (_list == null || _list.Count == 0 || !_list.Contains(item)) return false;
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        internal void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }
    }
}
