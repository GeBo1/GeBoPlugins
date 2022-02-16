#if (HS || PH || KK)
#define NEED_LOCKS
#endif
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using BepInEx;
using JetBrains.Annotations;
using KKAPI.Utilities;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    public class ExpiringSimpleCache<TKey, TValue> : SimpleCache<TKey, TValue>
    {
        private const int MinReadyFrame = 10;
        private readonly long _cacheDuration;

        private readonly AsyncWorker _expirationCheckWorker;

        private readonly CacheDictionary<TKey, float> _expirationTimes = new CacheDictionary<TKey, float>();
        private readonly AsyncWorker _expirationWorker;
        private readonly TransferList<TKey> _toExpire = new TransferList<TKey>();

        private readonly TransferList<ExpirationTime> _toUpdate = new TransferList<ExpirationTime>();
        private readonly AsyncWorker _updateExpirationWorker;

        private int _lateUpdateSkipCount;
        private float _minExpirationTime = float.MaxValue;

        public ExpiringSimpleCache(CacheDataLoader loader, TimeSpan cacheDuration, string cacheName = null) : this(
            loader, Math.Max(long.MaxValue, (long)cacheDuration.TotalSeconds), cacheName) { }

        [PublicAPI]
        public ExpiringSimpleCache(CacheDataLoader loader, long cacheDuration, string cacheName = null) : base(loader,
            cacheName)
        {
            _cacheDuration = cacheDuration;
            IsReady = Time.frameCount >= MinReadyFrame;
            _updateExpirationWorker = new AsyncWorker(AsyncUpdateExpirationWorker, ShouldExit);
            _expirationWorker = new AsyncWorker(AsyncRemoveExpiredEntriesWorker, ShouldExit);
            _expirationCheckWorker = new AsyncWorker(AsyncExpirationCheckWorker, ShouldExit);
            //CoroutineHelper.Start(RegisterExpirationCoroutine());
            CoroutineHelper.LateUpdate += LateUpdateHandler;
        }

        private float MinExpirationTime
        {
            get => _minExpirationTime;
            set
            {
                if (ThreadingHelper.Instance.InvokeRequired)
                {
                    throw new InvalidOperationException($"{nameof(MinExpirationTime)} must be updated on main Thread");
                }

                _minExpirationTime = value;
            }
        }

        private bool IsReady { get; set; }

        private bool ShouldExit()
        {
            return Disposed || Exiting;
        }

        private void UpdateMinExpirationTime()
        {
            if (ThreadingHelper.Instance.InvokeRequired)
            {
                var asyncAction = AsyncCalculateMinExpirationTime();
                ThreadingHelper.Instance.StartSyncInvoke(asyncAction);
            }
            else
            {
                ThreadingHelper.Instance.StartAsyncInvoke(AsyncCalculateMinExpirationTime);
            }


            Action AsyncCalculateMinExpirationTime()
            {
                float minExpirationTime;
                try
                {
#if NEED_LOCKS
                    using (_expirationTimes.Lock.GetDisposableReadOnlyLock())
#endif
                    {
                        minExpirationTime =
                            _expirationTimes.Count == 0 ? float.MaxValue : _expirationTimes.Values.Min();
                    }
                }
                catch
                {
                    minExpirationTime = MinExpirationTime;
                }

                return SyncSetMinExpirationTime;

                void SyncSetMinExpirationTime()
                {
                    MinExpirationTime = minExpirationTime;
                }
            }
        }

        private void LateUpdateHandler(object sender, EventArgs e)
        {
            if (ShouldExit()) return;
            if (!IsReady)
            {
                if (Time.frameCount < MinReadyFrame) return;
                IsReady = true;
            }

            if (GeBoAPI.Instance.IsHeavyLoad && _lateUpdateSkipCount++ < 3)
            {
                Logger?.LogDebug(
                    $"{this}:{nameof(LateUpdateHandler)}: heavy load detected, delaying expiration checks");
                return;
            }

            _lateUpdateSkipCount = 0;
            if (!_toUpdate.IsEmpty() && _updateExpirationWorker.Start()) return;
            if (!_toExpire.IsEmpty() && _expirationWorker.Start()) return;
            if (Time.realtimeSinceStartup > MinExpirationTime && _expirationTimes.Count > 0 && Count > 0 &&
                _expirationCheckWorker.Start()) { }
        }


        private Action AsyncExpirationCheckWorker()
        {
            var addedExpiration = false;
            if (!ShouldExit() && _toUpdate.IsEmpty())
            {
                // make a copy before attempting to process;
                var expirationTimes = _expirationTimes.ToList();

                foreach (var entry in expirationTimes.OrderBy(et => et.Value))
                {
                    if (ShouldExit()) return null;
                    // sorted by time, so exit early
                    if (entry.Value > Time.realtimeSinceStartup - 0.1f || !_toUpdate.IsEmpty()) break;
                    if (ContainsKey(entry.Key))
                    {
                        _toExpire.Add(entry.Key);
                        addedExpiration = true;
                    }

                    Thread.Sleep(1);
                }
            }

            if (!addedExpiration) UpdateMinExpirationTime();
            return null;
        }

        private Action AsyncRemoveExpiredEntriesWorker()
        {
            while (!ShouldExit() && _toUpdate.IsEmpty())
            {
                if (ShouldExit()) return null;
                // stop if pending updates
                if (!_toExpire.TryCollect(out var toExpire, 10)) break;
                foreach (var key in toExpire)
                {
                    if (ShouldExit()) return null;
                    // stop if pending updates
                    if (!_toUpdate.IsEmpty()) break;
                    if (_expirationTimes.TryGetValue(key, out var currentExpiration) &&
                        currentExpiration < Time.realtimeSinceStartup)
                    {
                        Remove(key);
                    }

                    Thread.Sleep(0);
                }

                Thread.Sleep(1);
            }

            UpdateMinExpirationTime();

            return null;
        }

        private Action AsyncUpdateExpirationWorker()
        {
            if (ShouldExit()) return null;

            while (_toUpdate.TryCollect(out var toRegister, 512))
            {
                foreach (var entry in toRegister)
                {
                    if (ShouldExit()) return null;
                    // cancel removal if pending
                    _toExpire.Remove(entry.Key);
                    _expirationTimes.AddOrUpdate(entry.Key, AddValueFactory, UpdateValueFactory);
                    Thread.Sleep(0);

                    float AddValueFactory(TKey _)
                    {
                        return entry.Time;
                    }

                    float UpdateValueFactory(TKey _, float currentValue)
                    {
                        return Mathf.Max(currentValue, entry.Time);
                    }
                }

                Thread.Sleep(1);
                if (ShouldExit()) return null;
            }

            UpdateMinExpirationTime();
            return null;
        }

        protected override void OnApplicationQuitting()
        {
            CoroutineHelper.LateUpdate -= LateUpdateHandler;
            _toExpire.Clear();
            _toUpdate.Clear();
            CoroutineHelper.StopAll();
            CoroutineHelper.Start(ShutdownCoroutine());
            _expirationTimes.Clear();
            base.OnApplicationQuitting();
        }

        protected override void OnReadFromCache(TKey key, TValue value)
        {
            base.OnReadFromCache(key, value);
            UpdateExpiration(key);
        }

        protected override void OnAddedToCache(TKey key, TValue value)
        {
            base.OnAddedToCache(key, value);
            UpdateExpiration(key);
        }


        private IEnumerator ShutdownCoroutine()
        {
            yield return CoroutineUtils.WaitForEndOfFrame;
            var endTime = Time.realtimeSinceStartup + (BlockingOperationTimeout * 2);
            var notified = false;
            var timedOut = false;
            while (_expirationCheckWorker.IsRunning || _updateExpirationWorker.IsRunning || _expirationWorker.IsRunning)
            {
                if (!notified)
                {
                    Logger?.LogInfoMessage($"{this}: waiting on expiration handler to complete");
                    notified = true;
                }

                if (Time.realtimeSinceStartup > endTime)
                {
                    timedOut = true;
                    break;
                }

                yield return null;
            }

            if (notified) Logger?.LogInfoMessage(timedOut ? $"{this}: forcing shutdown" : $"{this}: ready to shutdown");
            Logger?.LogDebug($"{this}: {nameof(ShutdownCoroutine)}: done");
            yield return null;
            CoroutineHelper.StopAll();
        }

        private void ThreadWaitUntil(float timeToWaitFor)
        {
            if (!ThreadingHelper.Instance.InvokeRequired)
            {
                throw new NotSupportedException($"{nameof(ThreadWaitUntil)} can not be called on main thread");
            }

            const int sleepTime = BlockingOperationTimeout / 4;
            while (!Exiting && !Disposed && Time.realtimeSinceStartup < timeToWaitFor)
            {
                Thread.Sleep(sleepTime);
            }
        }

        private IEnumerator CoroutineWaitUntilIsReady()
        {
            if (IsReady) yield break;
            while (!ShouldExit() && !IsReady)
            {
                yield return null;
            }
        }

        private void ThreadWaitUntilIsReady()
        {
            if (IsReady) return;
            if (!ThreadingHelper.Instance.InvokeRequired)
            {
                throw new NotSupportedException($"{nameof(ThreadWaitUntilIsReady)} can not be called on main thread");
            }

            const int sleepTime = BlockingOperationTimeout / 2;
            while (!ShouldExit() && !IsReady)
            {
                Thread.Sleep(sleepTime);
            }
        }

        private void UpdateExpiration(TKey key)
        {
            _toExpire.Remove(key);
            _toUpdate.Add(new ExpirationTime(key, Time.realtimeSinceStartup + _cacheDuration));
        }

        protected override void OnRemovedFromCache(TKey key, bool removed)
        {
            base.OnRemovedFromCache(key, removed);
            _expirationTimes.Remove(key);
        }

        protected override void OnCacheCleared()
        {
            base.OnCacheCleared();
            _expirationTimes.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CoroutineHelper.StopAll();
        }

        internal struct ExpirationTime
        {
            internal readonly TKey Key;
            internal readonly float Time;

            internal ExpirationTime(TKey key, float time)
            {
                Key = key;
                Time = time;
            }
        }
    }

    internal sealed class ExpirationCount
    {
        private long _count;

        internal long Value => Interlocked.Read(ref _count);

        internal long Increment()
        {
            return Interlocked.Increment(ref _count);
        }

        internal long Decrement()
        {
            return Interlocked.Decrement(ref _count);
        }
    }


    internal static class ExpiringSimpleCache
    {
        internal const int MinExpirationFrame = 5;
        private static bool _isPastMinExpirationFrame;

        internal static bool IsPastMinExpirationFrame =>
            _isPastMinExpirationFrame
                ? _isPastMinExpirationFrame
                : _isPastMinExpirationFrame = Time.frameCount > MinExpirationFrame;
    }
}
