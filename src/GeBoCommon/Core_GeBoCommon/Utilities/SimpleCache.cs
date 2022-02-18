using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using BepInEx.Logging;
using JetBrains.Annotations;
using KKAPI;
using UnityEngine;
#if DEBUG
using System.Collections;
#endif

namespace GeBoCommon.Utilities
{
    public class SimpleCache<TKey, TValue> : IDisposable
    {
        public delegate TValue CacheDataLoader(TKey key);

        protected const int BlockingOperationTimeout = 250;
        private readonly CacheDictionary<TKey, TValue> _cache = new CacheDictionary<TKey, TValue>(true);

        private readonly string _cacheName;

        private readonly SimpleLazy<CoroutineHelper> _coroutineHelperLoader;
        private readonly CacheDataLoader _loader;

        protected readonly HitMissCounter Stats;

        public SimpleCache(CacheDataLoader loader, string cacheName = null)
        {
            _coroutineHelperLoader = new SimpleLazy<CoroutineHelper>(InitCoroutineHelper);
            _cacheName = cacheName;
            if (_cacheName.IsNullOrEmpty())
            {
                try
                {
                    _cacheName = $"[{loader.Method.Name}]";
                }
                catch
                {
                    _cacheName = $"[{loader}]";
                }
            }

            Stats = new HitMissCounter(_cacheName);

            _loader = loader;

            KoikatuAPI.Quitting += ApplicationQuitting;
#if DEBUG
            CoroutineHelper.Start(DumpCacheStats());
#endif
        }
        //private readonly Dictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();

        public int Count => _cache.Count;

        protected ReaderWriterLockSlim Lock => _cache.Lock;

        protected CoroutineHelper CoroutineHelper => _coroutineHelperLoader.Value;

        protected bool Exiting => KoikatuAPI.IsQuitting;

        protected bool Disposed { get; private set; }

        protected ManualLogSource Logger => Common.CurrentLogger;


        public TValue this[TKey key] => Get(key);

        private CoroutineHelper InitCoroutineHelper()
        {
            return new CoroutineHelper();
        }

#if DEBUG
        private IEnumerator DumpCacheStats()
        {
            var nextNotify = 0f;
            while (!Disposed && !Exiting)
            {
                if (Time.realtimeSinceStartup > nextNotify)
                {
                    LogCacheStats(nameof(DumpCacheStats));
                    nextNotify = Time.realtimeSinceStartup + 15f;
                }

                yield return null;
            }
        }
#endif

        private void ApplicationQuitting(object sender, EventArgs e)
        {
            OnApplicationQuitting();
        }

        protected virtual void OnApplicationQuitting()
        {
            LogCacheStats(nameof(ApplicationQuitting));
            _cache.Clear();
        }

        protected void LogCacheStats(string prefix)
        {
            Logger?.LogDebug(Stats.GetCounts(prefix, Count));
        }



        public TValue Get(TKey key)
        {
            try
            {
                return DoGet(key);
            }
            catch (Exception err)
            {
                // Any non-cache related exception should be hit/thrown again when _loader is called below
                Logger.LogException(err, $"{this}: Unexpected error, bypassing caching (key={key})");
            }

            return _loader(key);
        }

        /// <summary>
        ///     Called when value added to cache, after lock released
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        protected virtual void OnAddedToCache(TKey key, TValue value)
        {
            Stats.RecordMiss();
        }


        /// <summary>
        ///     Called when value read from cache, lock released if taken
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        protected virtual void OnReadFromCache(TKey key, TValue value)
        {
            Stats.RecordHit();
        }

        /// <summary>
        ///     Called when cache cleared (after lock released)
        /// </summary>
        protected virtual void OnCacheCleared() { }

        /// <summary>
        ///     Called when key removed from cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="removed">if set to <c>true</c> key was removed.</param>
        protected virtual void OnRemovedFromCache(TKey key, bool removed) { }

        [PublicAPI]
        public void Clear()
        {
            _cache.Clear();
            OnCacheCleared();
        }

        public override string ToString()
        {
            return $"{_cacheName} ({this.GetPrettyTypeName()})";
        }


        protected TValue DoGet(TKey key)
        {
            if (Disposed || Exiting) return _loader(key);
            var loaderFired = false;
            var start = Time.realtimeSinceStartup;
            try
            {
                var result = _cache.GetOrAdd(key, LoaderHelper);
                if (loaderFired)
                {
                    OnAddedToCache(key, result);
                }
                else
                {
                    OnReadFromCache(key, result);
                }

                return result;

                TValue LoaderHelper(TKey innerKey)
                {
                    loaderFired = true;
                    return _loader(innerKey);
                }
            }
            finally
            {
                var elapsed = Time.realtimeSinceStartup - start;
                if (loaderFired)
                {
                    Stats.RecordMissTime(start);
                }
                else
                {
                    Stats.RecordHitTime(start);
                }
            }
        }

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public bool Remove(TKey key)
        {
            if (Disposed || Exiting) return false;
            var result = _cache.Remove(key);
            OnRemovedFromCache(key, result);
            return result;
        }

        protected bool ContainsKey(TKey key)
        {
            return _cache.ContainsKey(key);
        }

        [SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;
            Logger?.DebugLogDebug($"{this}: Dispose({disposing})");
            if (disposing)
            {
                _cache.Dispose();
            }

            Clear();
            Disposed = true;
        }

        ~SimpleCache()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose( false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose( true);
            GC.SuppressFinalize(this);
        }
    }
}
