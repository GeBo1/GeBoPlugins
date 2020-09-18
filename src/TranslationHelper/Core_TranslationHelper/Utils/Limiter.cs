using System.Collections;
using System.Threading;
using BepInEx.Logging;
using GeBoCommon.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TranslationHelperPlugin.Utils
{
    internal class Limiter
    {
        private readonly string _limiterName;
        private readonly long _limit;
        private long _current;
        private readonly IEnumerator _waitUntilBelowLimit;

        internal Limiter(long limit, string limiterName, bool resetOnSceneTransition = false)
        {
            _limit = limit;
            _limiterName = limiterName;
            _waitUntilBelowLimit = new WaitUntil(IsBelowLimit);
            if (resetOnSceneTransition) SceneManager.sceneUnloaded += SceneUnloaded;
            Reset();
        }

        internal static ManualLogSource Logger => TranslationHelper.Logger;
        private void SceneUnloaded(Scene arg0)
        {
            Reset();
        }

        private void Reset()
        {
            _current = 0;
        }

        internal bool IsAtLimit()
        {
            return Interlocked.Read(ref _current) >= _limit;
        }

        internal bool IsBelowLimit()
        {
            return !IsAtLimit();
        }

        internal IEnumerator Start()
        {
            var start = Time.unscaledTime;
            var limited = 0;
            long current;
            while (true)
            {
                current = Interlocked.Increment(ref _current);
                if (current <= _limit) break;
                Interlocked.Decrement(ref _current);
                limited++;
                yield return _waitUntilBelowLimit;
            }

            Logger.DebugLogDebug(
                $"Limiter {_limiterName} ready ({current}/{_limit}): limited: {limited}, delay: {Time.unscaledTime - start}");
        }

        internal IEnumerator End()
        {
            EndImmediately();
            yield break;
        }

        internal void EndImmediately()
        {
            var value = Interlocked.Decrement(ref _current);
            if (value < 0) Interlocked.CompareExchange(ref _current, 0, value);
            Logger.DebugLogDebug($"Limiter done {_limiterName} ({value}/{_limit})");
        }
    }
}
