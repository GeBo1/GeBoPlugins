#if deadcode
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using UniRx.Operators;

namespace GeBoCommon.Utilities
{
    public partial class SimpleCache<TKey, TValue> : IDisposable
    {
        private readonly CacheDictionary<TKey, TValue> _cache = new CacheDictionary<TKey, TValue>();
        //private readonly Dictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();

        public int Count => _cache.Count;

        protected ReaderWriterLockSlim Lock => _cache.Lock;

        #if deadcode
        private static Action<bool> NoBoolWrapper(Action action)
        {
            return delegate { action(); };
        }

        private static Func<bool, TResult> NoBoolWrapper<TResult>(Func<TResult> func)
        {
            return delegate { return func(); };
        }

        protected TResult DoSafeReadOperation<TResult>(Func<bool, TResult> func)
        {
            if (Disposed) return func(false);
            var readTaken = false;
            try

            {
                if (!_lock.IsUpgradeableReadLockHeld)
                {
                    while (!_lock.TryEnterUpgradeableReadLock(BlockingOperationTimeout))
                    {
                        if (Exiting || Disposed)
                        {
                            Logger.LogWarning(
                                $"{this}: {nameof(DoSafeReadOperation)}: shutdown or cache disposal detected while waiting on lock");
                            return func(false);
                        }
                    }
                    readTaken = true;
                }

                return func(readTaken);
            }
            finally
            {
                if (readTaken) _lock.ExitUpgradeableReadLock();
            }
        }

        protected void DoSafeReadOperation(Action<bool> action)
        {
            if (Disposed)
            {
                action(false);
                return;
            }
            var readTaken = false;

            try

            {
                if (!_lock.IsUpgradeableReadLockHeld)
                {
                    while (!_lock.TryEnterUpgradeableReadLock(BlockingOperationTimeout))
                    {
                        if (Exiting || Disposed)
                        {
                            Logger.LogWarning($"{this}: {nameof(DoSafeReadOperation)}: shutdown or cache disposal detected while waiting on lock");
                            action(false);
                            return;
                        }
                    }
                    readTaken = true;
                }

                action(readTaken);
            }
            finally
            {
                if (readTaken) _lock.ExitUpgradeableReadLock();
            }
        }

        /*
        protected IEnumerator DoNonBlockingSafeReadOperation(Func<bool, IEnumerator> func)
        {
            var readTaken = false;
            try
            {
                // NonBlocking ops should always take the lock again in-case it's released elsewhere by thread
                while (!_lock.TryEnterUpgradeableReadLock(0))
                {
                    yield return null;
                    if (Disposed) yield break;
                }

                readTaken = true;

                yield return func(readTaken);
            }
            finally
            {
                if (readTaken) _lock.ExitUpgradeableReadLock();
            }
        }
        */

        protected void DoSafeReadOperation(Action action)
        {
            DoSafeReadOperation(NoBoolWrapper(action));
        }

        protected TResult DoSafeReadOperation<TResult>(Func<TResult> func)
        {
            return DoSafeReadOperation(NoBoolWrapper(func));
        }

        /*
        protected IEnumerator DoNonBlockingSafeReadOperation(Func<IEnumerator> func)
        {
            yield return DoNonBlockingSafeReadOperation(NoBoolWrapper(func));
        }
        */

        protected void DoSafeWriteOperation(Action<bool> action)
        {
            if (Disposed)
            {
                action(false);
                return;
            }
            var writeTaken = false;
            try
            {
                if (!_lock.IsWriteLockHeld)
                {
                    while (!_lock.TryEnterWriteLock(BlockingOperationTimeout))
                    {
                        if (Exiting || Disposed)
                        {
                            Logger.LogWarning($"{this}: {nameof(DoSafeWriteOperation)}: shutdown or cache disposal detected while waiting on lock");
                            action(false);
                            return;
                        }
                    }
                    writeTaken = true;
                }

                action(writeTaken);
            }
            finally
            {
                if (writeTaken) _lock.ExitWriteLock();
            }
        }

        protected TResult DoSafeWriteOperation<TResult>(Func<bool, TResult> func)
        {
            if (Disposed) return func(false);
            var writeTaken = false;
            try
            {
                if (!_lock.IsWriteLockHeld)
                {
                    while (!_lock.TryEnterWriteLock(BlockingOperationTimeout))
                    {
                        if (Exiting || Disposed)
                        {
                            Logger.LogWarning($"{this}: {nameof(DoSafeWriteOperation)}: shutdown or cache disposal detected while waiting on lock");
                            return func(false);
                        }
                    }
                    writeTaken = true;
                }

                return func(writeTaken);
            }
            finally
            {
                if (writeTaken) _lock.ExitWriteLock();
            }
        }

        /*
        protected IEnumerator DoNonBlockingSafeWriteOperation(Func<bool, IEnumerator> func)
        {
            Logger.LogFatal($"{nameof(DoNonBlockingSafeWriteOperation)}: {func.Method.Name}");
            var writeTaken = false;
            try
            {
                // NonBlocking ops should always take the lock again in-case it's released elsewhere
                    while (!_lock.TryEnterWriteLock(0))
                    {
                        yield return null;
                        if (Disposed) yield break;
                        Logger.LogFatal($"{nameof(DoNonBlockingSafeWriteOperation)}: {func.Method.Name}: retry taking lock");
                    }
                    Logger.LogFatal($"{nameof(DoNonBlockingSafeWriteOperation)}: {func.Method.Name}: took lock");
                    writeTaken = true;
                Logger.LogFatal($"{nameof(DoNonBlockingSafeWriteOperation)}: {func.Method.Name}: calling, writeTaken={writeTaken}");
                yield return func(writeTaken);
            }
            finally
            {
                if (writeTaken)
                {
                    Logger.LogFatal($"{nameof(DoNonBlockingSafeWriteOperation)}: {func.Method.Name}: releasing lock");
                    _lock.ExitWriteLock();
                }
            }
        }
        */

        protected void DoSafeWriteOperation(Action action)
        {
            DoSafeWriteOperation(NoBoolWrapper(action));
        }

        protected TResult DoSafeWriteOperation<TResult>(Func<TResult> func)
        {
            return DoSafeWriteOperation(NoBoolWrapper(func));
        }

        /*
        protected IEnumerator DoNonBlockingSafeWriteOperation(Func<IEnumerator> func)
        {
            return DoNonBlockingSafeWriteOperation(NoBoolWrapper(func));
        }
        */

        private void ImplementationSpecificDispose(bool disposing)
        {
            if (!disposing) return;
            _lock.Dispose();
        }
#endif

      
    }
}
#endif
