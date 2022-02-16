using System;
using System.Threading;
using BepInEx;
using JetBrains.Annotations;
using KKAPI;
using UnityEngine;

namespace GeBoCommon.Utilities
{
    public class AsyncWorker
    {
        private const float FloatConverter = 1_000_000f;
        private readonly Func<bool> _exitTest;
        private readonly Func<Action> _workMethod;
        private long _lastFinished;

        private long _lastStarted;
        private long _locker;

        public AsyncWorker(Func<Action> workMethod, Func<bool> exitTest = null)
        {
            _workMethod = workMethod;
            _exitTest = exitTest;
        }

        [PublicAPI]
        public float LastStarted
        {
            get => Interlocked.Read(ref _lastStarted) / FloatConverter;
            private set => Interlocked.Exchange(ref _lastStarted, (long)Mathf.Floor(value * FloatConverter));
        }

        [PublicAPI]
        public float LastFinished
        {
            get => Interlocked.Read(ref _lastFinished) / FloatConverter;
            private set => Interlocked.Exchange(ref _lastFinished, (long)Mathf.Ceil(value * FloatConverter));
        }

        public bool IsRunning => Interlocked.Read(ref _locker) != 0;

        public bool Start()
        {
            if (IsRunning || ShouldExit()) return false;
            Common.CurrentLogger?.LogDebug($"{this}:{_workMethod.Method.Name}: starting");
            ThreadingHelper.Instance.StartAsyncInvoke(WorkerWrapper);
            return true;
        }

        private bool ShouldExit()
        {
            if (KoikatuAPI.IsQuitting)
            {
                GeBoAPI.Instance.SafeProc(i => i.Logger?.LogDebug($"stopping {this}"));
                return true;
            }

            return _exitTest != null && _exitTest();
        }

        private Action WorkerWrapper()
        {
            if (ShouldExit() || Interlocked.CompareExchange(ref _locker, 1, 0) != 0) return null;
            try
            {
                LastStarted = Time.realtimeSinceStartup;
                var result = _workMethod();
                if (result == null)
                {
                    Complete();
                    return null;
                }

                return () =>
                {
                    try
                    {
                        result();
                    }
                    finally
                    {
                        Complete();
                    }
                };
            }
            catch
            {
                Complete();
                throw;
            }
        }

        private void Complete()
        {
            LastFinished = Time.realtimeSinceStartup;
            Interlocked.Exchange(ref _locker, 0);
        }
    }
}
