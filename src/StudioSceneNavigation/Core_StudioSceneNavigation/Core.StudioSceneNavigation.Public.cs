using System;
using System.Collections;
using System.Collections.Generic;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI.Utilities;
using UnityEngine;

namespace StudioSceneNavigationPlugin
{
    partial class StudioSceneNavigation
    {
        public static class Public
        {
            [PublicAPI]
            public static bool LastLoadFailed { get; private set; }

            [PublicAPI]
            public static IEnumerator LoadSceneExternal(string scenePath)
            {
                var count = 0;
                LastLoadFailed = true;
                while (_externalLoadInProgress && count++ < 20) yield return null;
                if (_externalLoadInProgress)
                {
                    Logger.LogError(
                        $"{nameof(LoadSceneExternal)}: unable to load scene, external load currently in progress");
                    yield break;
                }

                _externalLoadInProgress = true;
                _setPage = false;
                Coroutine coroutine = null;
                var coroutines = ListPool<IEnumerator>.Get();
                try
                {
                    coroutines.Add(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(scenePath));
                    coroutines.Add(ExternalLoadComplete());
                    coroutine = Instance.StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));
                }
                catch (Exception err)
                {
                    _externalLoadInProgress = false;
                    Logger.LogFatal($"{nameof(LoadSceneExternal)}: Error loading {scenePath}: {err.Message}");
                    LastLoadFailed = true;
                }
                finally
                {
                    ListPool<IEnumerator>.Release(coroutines);
                }

                if (coroutine != null) yield return coroutine;

                IEnumerator ExternalLoadComplete()
                {
                    _externalLoadInProgress = false;
                    LastLoadFailed = false;

                    yield break;
                }
            }

            [PublicAPI]
            public static bool IsSceneMaybeValid(string path)
            {
                return Instance.IsSceneValid(path);
            }

            [PublicAPI]
            public static IEnumerable<string> GetScenePaths()
            {
                return ScenePaths.ToArray();
            }
        }
    }
}
