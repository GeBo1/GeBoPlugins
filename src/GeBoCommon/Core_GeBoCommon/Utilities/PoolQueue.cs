#if (HS || PH || KK)
#define NO_CONCURRENT
#endif
using BepInEx.Logging;
using JetBrains.Annotations;
#if NO_CONCURRENT
using System.Collections.Generic;
#else
using System.Collections.Concurrent;
#endif

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    internal abstract class BasePoolQueue<T>
    {
        protected static ManualLogSource Logger => Common.CurrentLogger;

        public abstract int Count { get; }
        public abstract bool IsEmpty { get; }
        public abstract bool TryDequeue(out T obj);
        public abstract void Enqueue(T obj);
        public abstract void Clear();

        public virtual int ReleaseObjects(int keep = -1)
        {
            var count = 0;
            while ((keep == -1 || Count > keep) && TryDequeue(out _)) count++;
            Logger?.DebugLogDebug($"{this.GetPrettyTypeName()}.{nameof(ReleaseObjects)}: released {count}");
            return count;
        }
    }
#if !NO_CONCURRENT
    internal class PoolQueue<T> : BasePoolQueue<T>
    {
        private ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public override bool IsEmpty => _queue.IsEmpty;
        public override int Count => _queue.Count;

        public override void Clear()
        {
            _queue = new ConcurrentQueue<T>();
        }

        public override bool TryDequeue(out T obj)
        {
            return _queue.TryDequeue(out obj);
        }

        public override void Enqueue(T obj)
        {
            _queue.Enqueue(obj);
        }
    }
#else
    internal class PoolQueue<T> : BasePoolQueue<T>
    {
        private readonly object _basicLock = new object();
        private readonly Queue<T> _queue = new Queue<T>();

        public override bool IsEmpty => Count == 0;

        public override int Count
        {
            get
            {
                lock (_basicLock)
                {
                    return _queue.Count;
                }
            }
        }

        public override bool TryDequeue(out T obj)
        {
            lock (_basicLock)
            {
                if (IsEmpty)
                {
                    obj = default;
                    return false;
                }

                obj = _queue.Dequeue();
                return true;
            }
        }

        public override void Enqueue(T obj)
        {
            lock (_basicLock)
            {
                _queue.Enqueue(obj);
            }
            
        }

        public override void Clear()
        {
            lock (_basicLock)
            {
                _queue.Clear();
            }
        }
    }
#endif
}
