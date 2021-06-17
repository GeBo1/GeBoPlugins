#if TIMERS
using System;
using System.Collections;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeBoCommon
{
    internal static partial class Timers
    {
        private static float _makerStartupStart;

        private static bool _hooked;
        private static ManualLogSource Logger => Common.CurrentLogger;

        internal static void Setup()
        {
            if (!_hooked)
            {
                _hooked = true;
                Harmony.CreateAndPatchAll(typeof(Hooks));
            }

            if (!GeBoAPI.TimersEnabled) return;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            if (StudioAPI.InsideStudio)
            {
                if (!StudioAPI.StudioLoaded) SceneManager.sceneLoaded += StudioStartupTimer;
            }
            else
            {
                MakerAPI.MakerStartedLoading += MakerStartupTimerStart;
            }
        }

        private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            Logger.LogDebug($"Scene={arg0}, mode={arg1}, listLoading={MakerAPI.CharaListIsLoading}");
        }

        private static void MakerStartupTimerStart(object sender, RegisterCustomControlsEvent e)
        {
            _makerStartupStart = Time.realtimeSinceStartup;
            MakerAPI.MakerFinishedLoading += MakerStartupTimer;
        }

        private static void MakerStartupTimer(object sender, EventArgs e)
        {
            LogTimerAtEndOfFrame(nameof(MakerStartupTimer), _makerStartupStart);
            MakerAPI.MakerFinishedLoading -= MakerStartupTimer;
        }

        private static void StudioStartupTimer(Scene arg0, LoadSceneMode arg1)
        {
            if (!StudioAPI.StudioLoaded) return;
            LogTimerAtEndOfFrame(nameof(StudioStartupTimer));
            SceneManager.sceneLoaded -= StudioStartupTimer;
        }

        private static void LogTimer(string name, float start = -1f, float end = -1f)
        {
            if (!GeBoAPI.TimersEnabled) return;
            if (end < 0f) end = Time.realtimeSinceStartup;
            var elapsed = start < 0 ? end : end - start;
            Logger.LogDebug($"{name}: {elapsed:000.0000000000}s");
        }

        private static void LogTimerAtEndOfFrame(string name, float start = -1f, float end = -1f)
        {
            if (!GeBoAPI.TimersEnabled) return;

            GeBoAPI.Instance.SafeProc(api => api.StartCoroutine(LogTimerAtEndOfFrameCoroutine(name, start, end)));
        }

        private static IEnumerator LogTimerAtEndOfFrameCoroutine(string name, float start, float end)
        {
            if (!GeBoAPI.TimersEnabled) yield break;
            yield return CoroutineUtils.WaitForEndOfFrame;
            LogTimer(name, start, end);
        }

        internal static partial class Hooks { }
    }
}
#endif
