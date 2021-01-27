using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    public class ExpiringSimpleCache<TKey, TValue> : SimpleCache<TKey, TValue>
    {
        private readonly int _cacheDuration;

        private readonly SimpleLazy<CoroutineHelper> _coroutineHelperLoader =
            new SimpleLazy<CoroutineHelper>(() => new CoroutineHelper());

        private readonly Dictionary<TKey, float> _expirationTimes = new Dictionary<TKey, float>();

        private IEnumerator _expirationHandler;

        // used to ensure only single handler running
        private long _expirationHandlerRunning;

        public ExpiringSimpleCache(CacheDataLoader loader, int cacheDuration) : base(loader)
        {
            _cacheDuration = cacheDuration;
        }

        protected CoroutineHelper CoroutineHelper => _coroutineHelperLoader.Value;

        private void StartExpirationHandler()
        {
            if (Disposed || _expirationHandler != null) return;
            if (Interlocked.CompareExchange(ref _expirationHandlerRunning, 1, 0) != 0) return;
            _expirationHandler = ExpirationHandler();
            CoroutineHelper.Start(_expirationHandler);
        }

        private void StopExpirationHandler()
        {
            if (Disposed || _expirationHandler == null) return;
            CoroutineHelper.Stop(_expirationHandler);
            Interlocked.Exchange(ref _expirationHandlerRunning, 0);
            _expirationHandler = null;
        }


        private IEnumerator ExpirationHandler()
        {
            var lockWait = new TimeSpan(1);
            while (Count > 0)
            {
                if (Disposed) yield break;
                var minExpirationTime = _expirationTimes.Values.Min();
                if (minExpirationTime > Time.realtimeSinceStartup)
                {
                    yield return new WaitForSecondsRealtime(
                        Mathf.Max(1f, minExpirationTime - Time.realtimeSinceStartup));
                }

                var readTaken = !Lock.IsUpgradeableReadLockHeld;
                if (readTaken)
                {
                    while (!Lock.TryEnterUpgradeableReadLock(lockWait)) yield return null;
                }

                try
                {
                    // only clean up so many per frame
                    var expiredKeys = _expirationTimes.Where(e => e.Value < Time.realtimeSinceStartup)
                        .Select(e => e.Key).Take(50).ToList();
                    if (expiredKeys.Count > 0)
                    {
                        var writeTaken = !Lock.IsWriteLockHeld;
                        if (writeTaken)
                        {
                            while (!Lock.TryEnterWriteLock(lockWait)) yield return null;
                        }

                        try
                        {
                            foreach (var key in expiredKeys)
                            {
                                Remove(key);
                            }
                        }
                        finally
                        {
                            if (writeTaken) Lock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    if (readTaken) Lock.ExitUpgradeableReadLock();
                }

                yield return null;
            }

            Interlocked.Exchange(ref _expirationHandlerRunning, 0);
            _expirationHandler = null;
        }

        protected override TValue DoGet(TKey key)
        {
            var getOk = false;
            if (Disposed) return base.DoGet(key);
            var readTaken = !Lock.IsUpgradeableReadLockHeld;
            if (readTaken) Lock.EnterUpgradeableReadLock();
            try
            {
                var result = base.DoGet(key);
                getOk = true;
                return result;
            }
            finally
            {
                // don't handle expiration if something went wrong with get above
                if (getOk)
                {
                    var writeTaken = !Lock.IsWriteLockHeld;
                    if (writeTaken) Lock.EnterWriteLock();
                    try
                    {
                        _expirationTimes[key] = Time.realtimeSinceStartup + _cacheDuration;
                        // if something went wrong don't start handler
                        StartExpirationHandler();
                    }
                    finally
                    {
                        if (writeTaken) Lock.ExitWriteLock();
                    }
                }

                if (readTaken) Lock.ExitUpgradeableReadLock();
            }
        }


        public override bool Remove(TKey key)
        {
            if (Disposed) return base.Remove(key);
            var writeTaken = !Lock.IsWriteLockHeld;
            if (writeTaken) Lock.EnterWriteLock();
            try
            {
                _expirationTimes.Remove(key);
                return base.Remove(key);
            }
            finally
            {
                if (writeTaken) Lock.ExitWriteLock();
            }
        }

        public override void Clear()
        {
            var writeTaken = !Lock.IsWriteLockHeld;
            if (writeTaken) Lock.EnterWriteLock();
            try
            {
                _expirationTimes.Clear();
                base.Clear();
                StopExpirationHandler();
            }
            finally
            {
                if (writeTaken) Lock.ExitWriteLock();
            }
        }
    }
}
