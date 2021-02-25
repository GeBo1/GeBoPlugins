using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BepInEx.Logging;
using JetBrains.Annotations;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    [PublicAPI]
    public abstract class ObjectPool
    {
        public const float CleanIdleSeconds = 60f;
        public const int CleanIdleKeepPercent = 75;
        public const int CleanIdleMinCount = 10;

        private static readonly HashSet<WeakReference> KnownPools = new HashSet<WeakReference>();
        private IEnumerator _cleanerHandler;

        // used to ensure only single handler running
        private long _cleanerRunning;
        private float _lastWentIdle;


        protected ObjectPool()
        {
            Logger?.DebugLogDebug($"{this.GetPrettyTypeName()}: new pool");
            KnownPools.Add(new WeakReference(this));
        }

        protected static ManualLogSource Logger => GeBoAPI.Instance != null ? GeBoAPI.Instance.Logger : null;


        public abstract int Count { get; }
        public abstract int InUseCount { get; }
        public abstract int IdleCount { get; }

        protected void StartIdle()
        {
            _lastWentIdle = Time.realtimeSinceStartup;
            StartCleaner();
        }

        protected void StartActive()
        {
            StopCleaner();
        }

        private void StartCleaner()
        {
            if (GeBoAPI.Instance == null || _cleanerHandler != null) return;
            if (Interlocked.CompareExchange(ref _cleanerRunning, 1, 0) != 0) return;
            _cleanerHandler = Cleaner();
            GeBoAPI.Instance.StartCoroutine(_cleanerHandler);
        }

        protected void StopCleaner()
        {
            if (GeBoAPI.Instance != null && _cleanerHandler != null)
            {
                GeBoAPI.Instance.StopCoroutine(_cleanerHandler);
            }

            Interlocked.Exchange(ref _cleanerRunning, 0);
            _cleanerHandler = null;
        }


        private IEnumerator Cleaner()
        {
            var startTime = Time.realtimeSinceStartup;
            var nextCleanTime = _lastWentIdle + CleanIdleSeconds;
            while (_lastWentIdle < startTime && Interlocked.Read(ref _cleanerRunning) > 0 &&
                   InUseCount == 0 && Count > CleanIdleMinCount)
            {
                var delay = nextCleanTime - Time.realtimeSinceStartup;
                if (delay > 0)
                {
                    delay = Mathf.Max(delay, 1f);
                    yield return new WaitForSecondsRealtime(delay);
                    continue;
                }

                var toKeep = Math.Max(Count * CleanIdleKeepPercent / 100, CleanIdleMinCount);
                if (toKeep <= CleanIdleMinCount) break;
                ClearIdle(toKeep);
                nextCleanTime = Time.realtimeSinceStartup + CleanIdleSeconds;
                yield return null;
            }

            Interlocked.Exchange(ref _cleanerRunning, 0);
            _cleanerHandler = null;
        }

        public abstract void ClearIdle(int keep = -1);

        ~ObjectPool()
        {
            KnownPools.Remove(new WeakReference(this));
        }

        public static void GlobalClearIdle(int keep = -1)
        {
            foreach (var poolRef in KnownPools)
            {
                if (poolRef.IsAlive && poolRef.Target is ObjectPool pool)
                {
                    pool.ClearIdle(keep);
                }
            }

            KnownPools.RemoveWhere(r => !r.IsAlive);
        }

        public static IEnumerator GlobalClearIdleCoroutine(int keep = -1, YieldInstruction delay = null)
        {
            // save pool list before delays
            KnownPools.RemoveWhere(r => !r.IsAlive);
            var pools = KnownPools.Select(r => r.Target as ObjectPool).Where(p => p != null).ToArray();
            foreach (var pool in pools)
            {
                pool.ClearIdle(keep);
                yield return delay;
            }
        }
    }

    [PublicAPI]
    public class ObjectPool<T> : ObjectPool where T : new()
    {
        private readonly Func<T> _initializer;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        private readonly BasePoolQueue<T> _queue = new PoolQueue<T>();
        private int _count;


        public ObjectPool(Action<T> onGet, Action<T> onRelease, Func<T> initializer = null)
        {
            _onGet = onGet;
            _onRelease = onRelease;
            _initializer = initializer;
        }

        public override int Count => _count;

        public override int InUseCount => _count - _queue.Count;

        public override int IdleCount => _queue.Count;

        public T Get()
        {
            T obj;
            if (!GeBoAPI.EnableObjectPools)
            {
                obj = _initializer != null ? _initializer() : new T();
                _onGet?.Invoke(obj);
                return obj;
            }

            var wasIdle = InUseCount == 0;

            if (!_queue.TryDequeue(out obj))
            {
                obj = _initializer != null ? _initializer() : new T();
                Interlocked.Increment(ref _count);
            }

            _onGet?.Invoke(obj);
            if (wasIdle) StartActive();
            return obj;
        }

        public void Release(T obj)
        {
            if (!GeBoAPI.EnableObjectPools) return;
            _queue.Enqueue(obj);
            _onRelease?.Invoke(obj);
            if (InUseCount == 0) StartIdle();
        }

        public override void ClearIdle(int keep = -1)
        {
            Interlocked.Add(ref _count, -1 * _queue.ReleaseObjects(keep));
        }
    }
}
