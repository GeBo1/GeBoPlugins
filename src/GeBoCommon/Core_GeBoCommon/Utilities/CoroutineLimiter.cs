using System.Collections;
using System.Threading;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Studio;
using KKAPI.Studio.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeBoCommon.Utilities
{
    public class CoroutineLimiter
    {
        private readonly string _limiterName;
        private readonly IEnumerator _waitUntilBelowLimit;
        private long _current;

        public CoroutineLimiter(long limit, string limiterName, bool resetOnSceneTransition = false)
        {
            Limit = limit;
            _limiterName = limiterName;
            _waitUntilBelowLimit = new WaitUntil(IsBelowLimit);
            if (resetOnSceneTransition) SceneManager.activeSceneChanged += ActiveSceneChanged;
            if (KoikatuAPI.GetCurrentGameMode() == GameMode.Studio)
            {
                StudioSaveLoadApi.SceneLoad += StudioSaveLoadApi_SceneLoad;
            }

            Reset();
        }

        public long Limit { get; }

        private static ManualLogSource Logger => GeBoAPI.Instance.Logger;

        private void StudioSaveLoadApi_SceneLoad(object sender, SceneLoadEventArgs e)
        {
            if (e.Operation != SceneOperationKind.Clear) return;
            Reset();
        }

        private void ActiveSceneChanged(Scene arg0, Scene arg1)
        {
            try
            {
                if (StudioAPI.InsideStudio) return;
            }
            catch { }

            Reset();
        }

        public void Reset()
        {
            _current = 0;
        }

        public bool IsAtLimit()
        {
            return Interlocked.Read(ref _current) >= Limit;
        }

        public bool IsBelowLimit()
        {
            return !IsAtLimit();
        }

        public IEnumerator Start()
        {
            var start = Time.realtimeSinceStartup;
            var startFrame = Time.renderedFrameCount;
            var limited = 0;
            long current;
            while (true)
            {
                current = Interlocked.Increment(ref _current);
                if (current <= Limit) break;
                Interlocked.Decrement(ref _current);
                limited++;
                yield return _waitUntilBelowLimit;
            }

            Logger.DebugLogDebug(
                $"CoroutineLimiter {_limiterName} ready ({current}/{Limit}): limited: {limited}, delay: {Time.realtimeSinceStartup - start:000.0000000000} ({Time.renderedFrameCount - startFrame} frames)");
        }

        public IEnumerator End()
        {
            EndImmediately();
            yield break;
        }

        public void EndImmediately()
        {
            var value = Interlocked.Decrement(ref _current);
            if (value < 0) Interlocked.CompareExchange(ref _current, 0, value);
            Logger.DebugLogDebug($"Limiter done {_limiterName} ({value}/{Limit})");
        }
    }
}
