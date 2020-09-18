using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using GeBoCommon.AutoTranslation;


namespace TranslationHelperPlugin
{
    public class NameTracker
    {
        private readonly NameScopeDictionary<NameScopeTracker> _tracker = new NameScopeDictionary<NameScopeTracker>();
        private static ManualLogSource _logger = null;
        internal static ManualLogSource Logger => _logger ?? (_logger = TranslationHelper.Logger);
        public void TrackName(NameScope scope, string name, IEnumerable<TranslationResultHandler> handlers)
        {
            _tracker[scope].TrackName(name, handlers);
        }

        public bool TryAddHandlers(NameScope scope, string name, IEnumerable<TranslationResultHandler> handlers)
        {
            return _tracker[scope].TryAddHandlers(name, handlers);
        }

        public void CallHandlers(NameScope scope, string name, ITranslationResult result)
        {
            _tracker[scope].CallHandlers(name, result);
        }

        public bool IsTracking(NameScope scope, string name)
        {
            return _tracker[scope].IsTracking(name);
        }

        public IEnumerator WaitUntilDoneTracking(NameScope scope, string name)
        {
            return _tracker[scope].WaitUntilDoneTracking(name);
        }

        internal class NameScopeTracker
        {
            private readonly object _lock = new object();

            private readonly Dictionary<string, List<TranslationResultHandler>> _tracker =
                new Dictionary<string, List<TranslationResultHandler>>();

            internal void TrackName(string name, IEnumerable<TranslationResultHandler> handlers)
            {
                lock (_lock)
                {
                    if (_tracker.ContainsKey(name))
                    {
                        throw new ArgumentException($"Name already tracked: {name}", nameof(name));
                    }
                    _tracker[name] = new List<TranslationResultHandler>(handlers);
                }
            }

            internal bool TryGetHandlers(string name, out List<TranslationResultHandler> handlers)
            {
                lock (_lock)
                {
                    return _tracker.TryGetValue(name, out handlers);
                }
            }

            internal bool TryAddHandlers(string name, IEnumerable<TranslationResultHandler> handlers)
            {
                lock (_lock)
                {
                    if (!TryGetHandlers(name, out var callbackHandlers)) return false;
                    callbackHandlers.AddRange(handlers);
                    return true;
                }
            }

            internal void CallHandlers(string name, ITranslationResult result)
            {
                var tmpHandlers = new List<TranslationResultHandler>();
                var emptyCount = 0;
                while (true)
                {
                    lock (_lock)
                    {

                        if (TryGetHandlers(name, out var handlers))
                        {
                            tmpHandlers.AddRange(handlers);
                            handlers.Clear();
                        }

                        if (emptyCount > 1 && tmpHandlers.Count == 0)
                        {
                            _tracker.Remove(name);
                            return;
                        }
                    }

                    if (tmpHandlers.Count == 0)
                    {
                        emptyCount++;
                        continue;
                    }
                    tmpHandlers.CallHandlers(result);
                    tmpHandlers.Clear();
                }
            }

            internal bool IsTracking(string name)
            {
                lock (_lock)
                {
                    return _tracker.ContainsKey(name);
                }
            }

            public IEnumerator WaitUntilDoneTracking(string name)
            {
                while (IsTracking(name))
                {
                    yield return null;
                    // avoid lock, outer loop will handle
                    // ReSharper disable once InconsistentlySynchronizedField
                    while (_tracker.ContainsKey(name))
                    {
                        yield return null;
                    }
                }
            }
        }
    }
}
