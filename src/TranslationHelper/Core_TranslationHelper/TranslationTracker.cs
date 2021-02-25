using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI.Utilities;

namespace TranslationHelperPlugin
{
    public class TranslationTracker
    {
        public delegate IEnumerator NameTranslationCoroutine(IEnumerable<TranslationResultHandler> handlers);

        private readonly List<IEnumerator> _outstandingCoroutines = new List<IEnumerator>();

        private readonly string _trackerName;
        private readonly NameScopeDictionary<NameScopeTracker> _trackers;

        public TranslationTracker(string trackerName, IEqualityComparer<string> comparer=null)
        {
            _trackerName = trackerName;
            var stringComparer = comparer ?? StringComparer.Ordinal;
            _trackers = new NameScopeDictionary<NameScopeTracker>(() => new NameScopeTracker(_trackerName, stringComparer));
            TranslationHelper.BehaviorChanged += TranslationHelper_BehaviorChanged;
        }

        [UsedImplicitly]
        public TranslationTracker() : this(nameof(TranslationTracker)) { }


        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private void TranslationHelper_BehaviorChanged(object sender, EventArgs e)
        {
            CancelOutstandingCoroutines();
            _trackers?.Clear();
        }

        private void CancelOutstandingCoroutines()
        {
            foreach (var coroutine in _outstandingCoroutines)
            {
                try
                {
                    Logger.DebugLogDebug($"canceling {coroutine}");
                    TranslationHelper.Instance.StartCoroutine(coroutine);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
                {
                    if (TranslationHelper.IsShuttingDown) continue;
                    Logger.LogWarning(
                        $"{_trackerName}.{nameof(CancelOutstandingCoroutines)}: unable to stop {coroutine}: {e.Message}");
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }

            _outstandingCoroutines.Clear();
        }

        [UsedImplicitly]
        public void TrackTranslationFunction(Action<IEnumerable<TranslationResultHandler>> translationFunction,
            NameScope scope, string trackedKey, IEnumerable<TranslationResultHandler> handlers)
        {
            TrackTranslationFunction(translationFunction, scope, trackedKey, handlers.ToArray());
        }

        public void TrackTranslationFunction(Action<IEnumerable<TranslationResultHandler>> translationFunction,
            NameScope scope, string trackedKey, params TranslationResultHandler[] handlers)
        {
            if (TryAddHandlers(scope, trackedKey, handlers)) return;
            TrackKey(scope, trackedKey, handlers);

            void CallbackHandler(ITranslationResult result)
            {
                CallHandlers(scope, trackedKey, result);
            }

            translationFunction(new TranslationResultHandler[] {CallbackHandler});
        }

        public IEnumerator TrackTranslationCoroutine(NameTranslationCoroutine translationCoroutine,
            NameScope scope, string trackedKey, IEnumerable<TranslationResultHandler> handlers)
        {
            return TrackTranslationCoroutine(translationCoroutine, scope, trackedKey, handlers.ToArray());
        }

        public IEnumerator TrackTranslationCoroutine(NameTranslationCoroutine translationCoroutine,
            NameScope scope, string trackedKey, params TranslationResultHandler[] handlers)
        {
            lock (GetLock(scope))
            {
                if (TryAddHandlers(scope, trackedKey, handlers))
                {
                    yield break;
                }

                TrackKey(scope, trackedKey, handlers);
            }

            void CallbackHandler(ITranslationResult result)
            {
                CallHandlers(scope, trackedKey, result);
            }

            yield return null; // allow requests later this frame to batch onto this one
            var tmp = translationCoroutine(new TranslationResultHandler[] {CallbackHandler});
            _outstandingCoroutines.Add(tmp);
            yield return TranslationHelper.Instance.StartCoroutine(tmp.AppendCo(() =>
                _outstandingCoroutines.Remove(tmp)));
        }

        private object GetLock(NameScope scope)
        {
            return _trackers[scope].GetLock();
        }

        public void TrackKey(NameScope scope, string trackedKey, IEnumerable<TranslationResultHandler> handlers)
        {
            TrackKey(scope, trackedKey, handlers.ToArray());
        }

        public void TrackKey(NameScope scope, string trackedKey, params TranslationResultHandler[] handlers)
        {
            Logger.DebugLogDebug(
                $"{_trackerName}.{nameof(TrackKey)}({scope}, {trackedKey}, handlers[{handlers.Length}])");
            _trackers[scope].TrackKey(trackedKey, handlers);
        }

        public bool TryAddHandlers(NameScope scope, string trackedKey, IEnumerable<TranslationResultHandler> handlers)
        {
            return TryAddHandlers(scope, trackedKey, handlers.ToArray());
        }

        public bool TryAddHandlers(NameScope scope, string trackedKey, params TranslationResultHandler[] handlers)
        {
            Logger.DebugLogDebug(
                $"{_trackerName}.{nameof(TryAddHandlers)}({scope}, {trackedKey}, handlers[{handlers.Length}])");
            return _trackers[scope].TryAddHandlers(trackedKey, handlers);
        }

        public void CallHandlers(NameScope scope, string trackedKey, ITranslationResult result)
        {
            _trackers[scope].CallHandlers(trackedKey, result);
        }

        [PublicAPI]
        public bool IsTracking(NameScope scope, string trackedKey)
        {
            return _trackers[scope].IsTracking(trackedKey);
        }

        [PublicAPI]
        public IEnumerator WaitUntilDoneTracking(NameScope scope, string trackedKey)
        {
            return _trackers[scope].WaitUntilDoneTracking(trackedKey);
        }

        internal class NameScopeTracker
        {
            private readonly object _lock = new object();

            private readonly Dictionary<string, List<TranslationResultHandler>> _tracker;

            private readonly string _trackerName;

            public NameScopeTracker(string trackerName, IEqualityComparer<string> comparer = null)
            {
                _trackerName = trackerName;
                _tracker = new Dictionary<string, List<TranslationResultHandler>>(comparer ?? StringComparer.Ordinal);
            }

            public NameScopeTracker() : this(nameof(TranslationTracker)) { }

            internal void TrackKey(string trackedKey, IEnumerable<TranslationResultHandler> handlers)
            {
                Logger.DebugLogDebug($"{_trackerName} {nameof(TrackKey)}({trackedKey}, {handlers})");
                lock (_lock)
                {
                    if (_tracker.ContainsKey(trackedKey))
                    {
                        throw new ArgumentException($"Name already tracked: {trackedKey}", nameof(trackedKey));
                    }

                    _tracker[trackedKey] = new List<TranslationResultHandler>(handlers);
                }
            }

            internal bool TryGetHandlers(string trackedKey, out List<TranslationResultHandler> handlers)
            {
                lock (_lock)
                {
                    return _tracker.TryGetValue(trackedKey, out handlers);
                }
            }

            internal bool TryAddHandlers(string trackedKey, IEnumerable<TranslationResultHandler> handlers)
            {
                Logger.DebugLogDebug($"{_trackerName} {nameof(TryAddHandlers)}({trackedKey}, {handlers})");
                lock (_lock)
                {
                    if (!TryGetHandlers(trackedKey, out var callbackHandlers)) return false;
                    callbackHandlers.AddRange(handlers);
                    return true;
                }
            }

            internal void CallHandlers(string trackedKey, ITranslationResult result)
            {
                //var tmpHandlers = new List<TranslationResultHandler>();
                var tmpHandlers = ListPool<TranslationResultHandler>.Get();
                var emptyCount = 0;
                var handlerCount = 0;
                var hitCount = 0;
                try
                {
                    while (true)
                    {

                        lock (_lock)
                        {
                            if (TryGetHandlers(trackedKey, out var handlers))
                            {
                                tmpHandlers.AddRange(handlers);
                                handlers.Clear();
                            }

                            if (emptyCount > 1 && tmpHandlers.Count == 0)
                            {
                                _tracker.Remove(trackedKey);
                                Logger.DebugLogDebug(
                                    $"{_trackerName}.{nameof(CallHandlers)}: {handlerCount} handlers called for {trackedKey} ({hitCount})");
                                return;
                            }
                        }

                        if (tmpHandlers.Count == 0)
                        {
                            emptyCount++;
                            continue;
                        }

                        // got handlers, reset empty count
                        emptyCount = 0;
                        hitCount++;

                        handlerCount += tmpHandlers.Count;
                        tmpHandlers.CallHandlers(result);

                        tmpHandlers.Clear();
                    }
                }
                finally
                {
                    ListPool<TranslationResultHandler>.Release(tmpHandlers);
                }
            }

            internal bool IsTracking(string trackedKey)
            {
                lock (_lock)
                {
                    return _tracker.ContainsKey(trackedKey);
                }
            }

            public IEnumerator WaitUntilDoneTracking(string trackedKey)
            {
                while (IsTracking(trackedKey))
                {
                    yield return null;
                    // avoid lock, outer loop will handle
                    // ReSharper disable once InconsistentlySynchronizedField
                    while (_tracker.ContainsKey(trackedKey))
                    {
                        yield return null;
                    }
                }
            }

            internal object GetLock()
            {
                return _lock;
            }
        }
    }
}
