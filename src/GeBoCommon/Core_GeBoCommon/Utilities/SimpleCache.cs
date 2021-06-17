using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using BepInEx.Logging;

namespace GeBoCommon.Utilities
{
    public class SimpleCache<TKey, TValue> : IDisposable
    {
        public delegate TValue CacheDataLoader(TKey key);

        private readonly Dictionary<TKey, TValue> _cache;
        private readonly CacheDataLoader _loader;
        protected readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        public SimpleCache(CacheDataLoader loader)
        {
            _cache = new Dictionary<TKey, TValue>();
            _loader = loader;
        }

        protected bool Disposed { get; private set; }

        protected ManualLogSource Logger => Common.CurrentLogger;
        public int Count => _cache.Count;

        public TValue this[TKey key] => Get(key);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
        protected virtual void Dispose(bool disposing)
        {
            Logger?.DebugLogDebug($"Dispose({disposing}): {this.GetPrettyTypeFullName()}");
            if (Disposed) return;
            Disposed = true;
            if (!disposing) return;
            Clear();
            Lock.Dispose();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public TValue Get(TKey key)
        {
            try
            {
                return DoGet(key);
            }
            catch (Exception err)
            {
                // Any non-cache related exception should be hit again when _loader is called below
                Logger.LogException(err, $"Unexpected error, bypassing {this.GetPrettyTypeName()} caching");
            }

            return _loader(key);
        }

        protected virtual TValue DoGet(TKey key)
        {
            if (Disposed) return _loader(key);
            var readTaken = !Lock.IsUpgradeableReadLockHeld;
            if (readTaken) Lock.EnterUpgradeableReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var cachedResult)) return cachedResult;

                var writeTaken = !Lock.IsWriteLockHeld;
                if (writeTaken) Lock.EnterWriteLock();
                try
                {
                    return _cache[key] = _loader(key);
                }
                finally
                {
                    if (writeTaken) Lock.ExitWriteLock();
                }
            }
            finally
            {
                if (readTaken) Lock.ExitUpgradeableReadLock();
            }
        }

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public virtual bool Remove(TKey key)
        {
            if (Disposed) return false;
            var writeTaken = !Lock.IsWriteLockHeld;
            if (writeTaken) Lock.EnterWriteLock();

            try
            {
                return _cache.Remove(key);
            }
            finally
            {
                if (writeTaken) Lock.ExitWriteLock();
            }
        }

        public virtual void Clear()
        {
            var writeTaken = !Lock.IsWriteLockHeld;
            if (writeTaken) Lock.EnterWriteLock();
            try
            {
                _cache.Clear();
            }
            finally
            {
                if (writeTaken) Lock.ExitWriteLock();
            }
        }
    }
}
