#if deadcode
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace GeBoCommon.Utilities
{
    public partial class SimpleCache<TKey, TValue> : IDisposable
    {
        // use SimpleLazy to ensure loader doesn't run concurrently
        private readonly ConcurrentDictionary<TKey, SimpleLazy<TValue>> _cache =
            new ConcurrentDictionary<TKey, SimpleLazy<TValue>>();

        public int Count => _cache.Count;

        protected bool IsReadSafe()
        {
            return true;
        }

        protected bool IsWriteSafe()
        {
            return true;
        }

        protected TResult DoSafeReadOperation<TResult>(Func<bool, TResult> func)
        {
            return func(false);
        }

        protected void DoSafeReadOperation(Action<bool> action)
        {
            action(false);
        }

        protected IEnumerator DoNonBlockingSafeReadOperation(Func<bool, IEnumerator> func)
        {
            return func(false);
        }

        protected void DoSafeReadOperation(Action action)
        {
            action();
        }

        protected TResult DoSafeReadOperation<TResult>(Func<TResult> func)
        {
            return func();
        }

        protected IEnumerator DoNonBlockingSafeReadOperation(Func<IEnumerator> func)
        {
            return func();
        }

        protected void DoSafeWriteOperation(Action<bool> action)
        {
            action(false);
        }

        protected TResult DoSafeWriteOperation<TResult>(Func<bool, TResult> func)
        {
            return func(false);
        }

        protected IEnumerator DoNonBlockingSafeWriteOperation(Func<bool, IEnumerator> func)
        {
            return func(false);
        }

        protected void DoSafeWriteOperation(Action action)
        {
            action();
        }

        protected TResult DoSafeWriteOperation<TResult>(Func<TResult> func)
        {
            return func();
        }

        protected IEnumerator DoNonBlockingSafeWriteOperation(Func<IEnumerator> func)
        {
            return func();
        }


        private void ImplementationSpecificDispose(bool disposing) { }

        protected bool UnsafeTryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var lazyValue))
            {
                value = lazyValue.Value;
                OnReadFromCache(key, value);
                return true;
            }

            value = default;
            return false;
        }

        protected virtual TValue DoGet(TKey key)
        {
            if (Disposed) return _loader(key);
            var loaderFired = false;

            var result = _cache.GetOrAdd(key, new SimpleLazy<TValue>(LoaderWrapper));
            if (loaderFired)
            {
                OnAddingToCache(key, result.Value);
            }
            else
            {
                OnReadFromCache(key, result.Value);
            }

            return result.Value;

            TValue LoaderWrapper()
            {
                loaderFired = true;
                return _loader(key);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
        public virtual bool Remove(TKey key)
        {
            if (Disposed) return false;
            return _cache.TryRemove(key, out _);
        }
    }
}
#endif
