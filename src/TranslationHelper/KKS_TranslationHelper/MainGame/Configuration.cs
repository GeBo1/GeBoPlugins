using System;
using System.Collections;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using Manager;
using SaveData;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.Assertions;
using GeBoCommon.Utilities;

namespace TranslationHelperPlugin.MainGame
{
    internal static partial class Configuration
    {
        internal static void GameSpecificSetup(Harmony harmony)
        {
            Assert.IsNotNull(harmony);
            TranslationHelper.CardTranslationBehaviorChanged += KKS_CardTranslationBehaviorChanged;
            PeriodChange += KKS_PeriodChange;
            StartH += KKS_StartH;
        }


        private static void KKS_StartH(object sender, HSceneEventArgs e)
        {
            Logger.DebugLogDebug($"{nameof(KKS_StartH)}: {e.Flag}");
            if (e.Flag == null) return;
            StartCoroutine(TranslateHNames(e.Flag, 3));
        }

        private static void KKS_PeriodChange(object sender, PeriodChangeEventArgs e)
        {
            StartCoroutine(TranslateGameNames());
        }

        private static void KKS_CardTranslationBehaviorChanged(object sender, EventArgs e)
        {
            StartCoroutine(TranslateGameNames(2));
        }

        internal static IEnumerator TranslateCharaDataNames(CharaData charaData, bool includeRelatedChaFiles = false)
        {
            Logger.DebugLogDebug($"{nameof(TranslateCharaDataNames)}: {charaData.Name}: START");
            if (TranslationHelper.Instance == null || TranslationHelper.Instance.CurrentGameMode != GameMode.MainGame ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled || charaData == null)
            {
                yield break;
            }

            while (TranslationHelper.Instance.CurrentGameMode == GameMode.MainGame &&
                   (!charaData.charFileInitialized || charaData.charFile == null))
            {
                yield return null;
            }

            if (TranslationHelper.Instance.CurrentGameMode != GameMode.MainGame) yield break;
            var jobs = ListPool<Coroutine>.Get();
            try
            {
                jobs.Add(StartCoroutine(charaData.charFile.TranslateCardNamesCoroutine()));

                if (includeRelatedChaFiles)
                {
                    if (charaData is Player player)
                    {
                        foreach (var playerChaFile in player.GetRelatedChaFiles())
                        {
                            if (playerChaFile == charaData.charFile) continue;
                            jobs.Add(StartCoroutine(playerChaFile.TranslateCardNamesCoroutine()));
                        }
                    }
                    else if (charaData is Heroine heroine)
                    {
                        foreach (var heroineChaFile in heroine.GetRelatedChaFiles())
                        {
                            if (heroineChaFile == charaData.charFile) continue;
                            jobs.Add(StartCoroutine(heroineChaFile.TranslateCardNamesCoroutine()));
                        }
                    }
                }

                Logger.DebugLogDebug($"{nameof(TranslateCharaDataNames)}: {charaData.Name}: job count={jobs.Count}");
                foreach (var job in jobs) yield return job;
            }
            finally
            {
                ListPool<Coroutine>.Release(jobs);
            }
        }


        internal static IEnumerator TranslateHNames(HFlag hFlag, int framesToDelay = 0)
        {
            var count = 0;
            var start = Time.realtimeSinceStartup;
            Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");
            try
            {
                for (var i = 0; i < framesToDelay; i++) yield return null;
                Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");


                while (hFlag.lstHeroine == null) yield return null;
                Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");

                var timeOut = Time.realtimeSinceStartup + 10f;
                while (hFlag.lstHeroine.Count < 1)
                {
                    yield return null;
                    if (Time.realtimeSinceStartup > timeOut) yield break;
                }

                Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");

                var stableFrames = 0;
                var lastCount = -1;

                while (stableFrames < 5)
                {
                    if (lastCount == hFlag.lstHeroine.Count)
                    {
                        stableFrames++;
                    }
                    else
                    {
                        lastCount = hFlag.lstHeroine.Count;
                        stableFrames = 0;
                    }

                    yield return null;
                    if (Time.realtimeSinceStartup > timeOut) yield break;
                }

                Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");

                var jobs = ListPool<Coroutine>.Get();
                try
                {
                    foreach (var heroine in hFlag.lstHeroine)
                    {
                        jobs.Add(StartCoroutine(TranslateCharaDataNames(heroine, true)));
                    }

                    timeOut = Time.realtimeSinceStartup + 2f;
                    while (hFlag.player == null || !hFlag.player.charFileInitialized || hFlag.player.charFile == null)
                    {
                        if (Time.realtimeSinceStartup > timeOut) break;
                        yield return null;
                    }

                    Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");

                    if (hFlag.player != null) jobs.Add(StartCoroutine(TranslateCharaDataNames(hFlag.player, true)));


                    foreach (var job in jobs) yield return job;
                    Logger.DebugLogDebug($"{nameof(TranslateHNames)}: {count++}: {Time.realtimeSinceStartup - start:0.000}");
                }
                finally
                {
                    ListPool<Coroutine>.Release(jobs);
                }
            }
            finally
            {
                Logger.DebugLogDebug(
                    $"{nameof(TranslateHNames)} Complete ({Time.realtimeSinceStartup - start:0.000} seconds)");
            }
        }

        internal static IEnumerator TranslateGameNames(int framesToDelay = 0)
        {
            var start = Time.realtimeSinceStartup;
            try
            {
                for (var i = 0; i < framesToDelay; i++) yield return null;
                while (!SingletonInitializer<Game>.initialized || Game.Player == null || Game.HeroineList == null)
                {
                    yield return null;
                    if (TranslationHelper.Instance.CurrentGameMode != GameMode.MainGame) yield break;
                }

                var jobs = ListPool<Coroutine>.Get();
                try
                {
                    jobs.Add(StartCoroutine(TranslateCharaDataNames(Game.Player, true)));

                    var timeOut = Time.realtimeSinceStartup + 5f;
                    while (Game.HeroineList.Count < 1)
                    {
                        yield return null;
                        if (Time.realtimeSinceStartup > timeOut) yield break;
                        if (TranslationHelper.Instance.CurrentGameMode != GameMode.MainGame) yield break;
                    }

                    var stableFrames = 0;
                    var lastCount = -1;

                    while (stableFrames < 5)
                    {
                        if (lastCount == Game.HeroineList.Count)
                        {
                            stableFrames++;
                        }
                        else
                        {
                            lastCount = Game.HeroineList.Count;
                            stableFrames = 0;
                        }

                        yield return null;
                        if (TranslationHelper.Instance.CurrentGameMode != GameMode.MainGame) yield break;
                        if (Time.realtimeSinceStartup > timeOut) yield break;
                    }

                    foreach (var heroine in Game.HeroineList)
                    {
                        jobs.Add(StartCoroutine(TranslateCharaDataNames(heroine, true)));
                    }

                    foreach (var job in jobs) yield return job;
                }
                finally
                {
                    ListPool<Coroutine>.Release(jobs);
                }
            }
            finally
            {
                Logger.DebugLogDebug(
                    $"{nameof(TranslateGameNames)} Complete ({Time.realtimeSinceStartup - start:0.000} seconds)");
            }
        }
    }
}
