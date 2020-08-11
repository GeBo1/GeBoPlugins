using System.Collections;
using System.Threading;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TranslationHelperPlugin.Utils
{
    internal class Limiter
    {
        private readonly long _limit;
        private long _current;

        internal Limiter(long limit, bool resetOnSceneTransition = false)
        {
            _limit = limit;
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
            if (IsAtLimit())
            {
                var start = Time.unscaledTime;
                yield return new WaitUntil(IsBelowLimit);
                Logger.LogDebug($"Limiter delay: ${Time.unscaledTime - start}");
            }

            Interlocked.Increment(ref _current);
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
        }
    }
}
