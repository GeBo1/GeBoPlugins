using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI.Utilities;
using Studio;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Utils;
using UnityEngine;

#if AI||HS2
using AIChara;

#endif

namespace TranslationHelperPlugin
{
    internal class CharaFileInfoTranslationManager
    {
        private static readonly CacheFunctionHelper CacheRecentTranslationsHelper = new CacheFunctionHelper();

        internal static readonly NameScopeDictionary<Dictionary<string, string>> RecentTranslationsByPath =
            new NameScopeDictionary<Dictionary<string, string>>(TranslationHelper.PathCacheInitializer);

        private static readonly CoroutineLimiter TranslateFileInfoLimiter =
            new CoroutineLimiter(300, nameof(TranslateFileInfoLimiter), true);

        private readonly HashSet<string> _pathsInProgress;
        private readonly TranslationTracker _pathTracker;

        private readonly IEnumerator _waitWhileFileInfosInProgress;

        internal CharaFileInfoTranslationManager()
        {
            _pathTracker = new TranslationTracker($"{nameof(CharaFileInfoTranslationManager)}.{nameof(_pathTracker)}",
                PathUtils.NormalizedPathComparer);
            _pathsInProgress = new HashSet<string>();
            _waitWhileFileInfosInProgress = new WaitWhile(AreFileInfosInProgress);
            TranslationHelper.CardTranslationBehaviorChanged += CardTranslationHelperBehaviorChanged;
            ExtendedSave.CardBeingSaved += CardBeingSaved;
        }

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private void CardBeingSaved(ChaFile file)
        {
            foreach (var scope in RecentTranslationsByPath.GetScopes())
            {
                var toRemove = RecentTranslationsByPath[scope].Keys
                    .Where(k => Path.GetFileName(k) == file.charaFileName).ToList();
                foreach (var path in toRemove) RecentTranslationsByPath[scope].Remove(path);
            }

            _pathsInProgress.RemoveWhere(p => Path.GetFileName(p) == file.charaFileName);
        }

        private void CardTranslationHelperBehaviorChanged(object sender, EventArgs e)
        {
            ClearCaches();
        }

        public void ClearCaches()
        {
            CacheRecentTranslationsHelper?.Clear();
            RecentTranslationsByPath?.Clear();
            _pathsInProgress?.Clear();
        }

        public bool AreFileInfosInProgress()
        {
            return _pathsInProgress.Count > 0;
        }

        [PublicAPI]
        public IEnumerator WaitOnFileInfoTranslations()
        {
            yield return _waitWhileFileInfosInProgress;
        }

        [PublicAPI]
        public static bool FileInfoNeedsTranslation(ICharaFileInfo fileInfo)
        {
            return StringUtils.ContainsJapaneseChar(fileInfo.Name);
        }

        [PublicAPI]
        public IEnumerator WaitOnFileInfo(ICharaFileInfo fileInfo)
        {
            if (fileInfo == null) yield break;

            var path = fileInfo.FullPath;
            var origName = fileInfo.Name;
            var sex = fileInfo.Sex;

            bool NotDone()
            {
                return _pathsInProgress.Contains(path);
            }

            // delay once if card does not appear active yet
            if (!NotDone()) yield return null;

            yield return new WaitWhile(NotDone);
            if (sex != CharacterSex.Unspecified && fileInfo.FullPath == path) ApplyTranslations(fileInfo);
            Logger.DebugLogDebug($"WaitOnFileInfo: {fileInfo}: done: {path}");
        }

        private static void ApplyTranslations(ICharaFileInfo fileInfo)
        {
            var scope = new NameScope(fileInfo.Sex);
            if (scope.Sex == CharacterSex.Unspecified) return;
            if (!TryGetRecentTranslation(fileInfo, out var result) || result.IsNullOrEmpty()) return;
            fileInfo.Name = result;
        }

        public static bool TryGetRecentTranslation(ICharaFileInfo fileInfo, out string result)
        {
            result = null;
#if !CORRUPT_CACHE_CAUSING_DUPLICATE_NAMES_FIXED
            return false;
#else
            var hit = RecentTranslationsByPath[new NameScope(fileInfo.Sex)].TryGetValue(fileInfo.FullPath, out result);
            Logger.DebugLogDebug(
                $"CharaFileInfoTranslationManager.TryGetRecentTranslation({fileInfo.Sex}, {fileInfo.FullPath}) => {result} {hit}");
            return hit;
#endif
        }

        public static bool TryGetRecentTranslation(NameScope scope, string path, out string result)
        {
            result = null;
#if !CORRUPT_CACHE_CAUSING_DUPLICATE_NAMES_FIXED
            return false;
#else
            path = PathUtils.NormalizePath(path);

            var hit = RecentTranslationsByPath[scope].TryGetValue(path, out result);
            Logger.DebugLogDebug(
                $"CharaFileInfoTranslationManager.TryGetRecentTranslation({scope}, {path}) => {result} {hit}");
            return hit;
#endif
        }

        public static void CacheRecentTranslation(NameScope scope, string path, string translatedName,
            bool overwriteExisting = true)
        {
            return;

#if CORRUPT_CACHE_CAUSING_DUPLICATE_NAMES_FIXED
            
            // ReSharper disable RedundantAssignment - used in DEBUG
            var added = false;
            var start = Time.realtimeSinceStartup;
            // ReSharper restore RedundantAssignment
            var key = new {path, translatedName};
            try
            {
                if (CacheRecentTranslationsHelper.WasCalledRecently(key)) return;
                Logger.DebugLogDebug(
                    $"CharaFileInfoTranslationManager.CacheRecentTranslation({scope}, {path}, {translatedName})");
                if (scope.Sex == CharacterSex.Unspecified || translatedName.IsNullOrEmpty() ||
                    path.IsNullOrEmpty())
                {
                    return;
                }

                // this is less "safe" than the other version, so bail if we've already got cache data
                if (!overwriteExisting && RecentTranslationsByPath[scope].ContainsKey(path)) return;

                // this is the slowest check, keep until last
                if (StringUtils.ContainsJapaneseChar(translatedName)) return;

                RecentTranslationsByPath[scope][PathUtils.NormalizePath(path)] = translatedName;
                // ReSharper disable once RedundantAssignment - used in DEBUG
                added = true;
            }
            finally
            {
                CacheRecentTranslationsHelper.RecordCall(key);
                Logger.DebugLogDebug(
                    $"CardFileInfoTranslationManager.CacheRecentTranslation(path): {Time.realtimeSinceStartup - start:000.0000000000}: added={added}");
            }
#endif
        }

        internal static Action<string> MakeCachingCallbackWrapper(ICharaFileInfo fileInfo,
            Action<string> callback = null)
        {
            var sex = fileInfo.Sex;
            var origName = fileInfo.Name;
            var path = fileInfo.FullPath;

            void ICharaFileInfoCachingCallbackWrapper(string translationResult)
            {
                Logger.DebugLogDebug(
                    $"{nameof(ICharaFileInfoCachingCallbackWrapper)}: translationResult={translationResult}, origName={origName}, path={path}, callback={callback}");
                if (!translationResult.IsNullOrEmpty() &&
                    !TranslationHelper.NameStringComparer.Equals(translationResult, origName) &&
                    !path.IsNullOrEmpty())
                {
                    CacheRecentTranslation(new NameScope(sex), path, translationResult, false);
                }

                callback?.Invoke(translationResult);
            }

            return ICharaFileInfoCachingCallbackWrapper;
        }

        public static Action<string> MakeCachingCallbackWrapper(string origName, CharaFileInfo charaFileInfo,
            NameScope scope, Action<string> callback = null)
        {
            var path = charaFileInfo.file;

            void CharaFileInfoCachingCallbackWrapper(string translationResult)
            {
                Logger.DebugLogDebug(
                    $"{nameof(CharaFileInfoCachingCallbackWrapper)}: translationResult={translationResult}, origName={origName}, path={path}, callback={callback}");
                if (!translationResult.IsNullOrEmpty() &&
                    !TranslationHelper.NameStringComparer.Equals(translationResult, origName) &&
                    !path.IsNullOrEmpty())
                {
                    CacheRecentTranslation(scope, path, translationResult);
                }

                callback?.Invoke(translationResult);
            }

            return CharaFileInfoCachingCallbackWrapper;
        }

        public static Action<string> MakeCachingCallbackWrapper(string origName, ChaFile chaFile, NameScope scope,
            Action<string> callback = null)
        {
            var fullPath = chaFile.GetFullPath();

            void ChaFileCachingCallbackWrapper(string translationResult)
            {
                Logger.DebugLogDebug(
                    $"{nameof(ChaFileCachingCallbackWrapper)}: translationResult={translationResult}, origName={origName}, path={fullPath}, callback={callback}");
                if (!translationResult.IsNullOrEmpty() &&
                    !TranslationHelper.NameStringComparer.Equals(translationResult, origName))
                {
                    if (!string.IsNullOrEmpty(fullPath))
                    {
                        CacheRecentTranslation(scope, fullPath, translationResult);
                    }
                }

                callback?.Invoke(translationResult);
            }

            return ChaFileCachingCallbackWrapper;
        }


        public IEnumerator TranslateFileInfo(ICharaFileInfo fileInfo, params TranslationResultHandler[] callbacks)
        {
            var scope = new NameScope(fileInfo.Sex);
            var path = fileInfo.FullPath;
            var originalName = fileInfo.Name;
            var done = false;
            var tmpCallbacks = callbacks.ToList();

            _pathsInProgress.Add(path);


            var cacheWrapper = MakeCachingCallbackWrapper(fileInfo);

            void DoneHandler(ITranslationResult result)
            {
                _pathsInProgress.Remove(path);
                if (result.Succeeded)
                {
                    cacheWrapper(result.TranslatedText);
                    CardNameTranslationManager.CacheRecentTranslation(scope, originalName, result.TranslatedText);
                    // must be set after CacheRecentTranslation   
                    fileInfo.SafeNameUpdate(path, originalName, result.TranslatedText);
                }

                done = true;
            }

            tmpCallbacks.Add(DoneHandler);
            tmpCallbacks.Add(Handlers.AddNameToAutoTranslationCache(originalName));

            IEnumerator WhileNotDone()
            {
                while (!done)
                {
                    yield return null;
                }
            }

            IEnumerator TranslationCoroutine(IEnumerable<TranslationResultHandler> handlers)
            {
                if (TryGetRecentTranslation(scope, path, out var cachedName))
                {
                    var result = new TranslationResult(originalName, cachedName);
                    if (result.Succeeded) fileInfo.SafeNameUpdate(path, originalName, cachedName);

                    handlers.CallHandlers(result);
                    yield break;
                }

                yield return null;

                if (!StringUtils.ContainsJapaneseChar(originalName))
                {
                    handlers.CallHandlers(new TranslationResult(false, originalName));
                    yield break;
                }

                var tmpHandlers = handlers.ToList();
                tmpHandlers.Add(_ => TranslateFileInfoLimiter.EndImmediately());
                yield return TranslationHelper.Instance.StartCoroutine(
                    TranslateFileInfoLimiter.Start().AppendCo(
                        CardNameTranslationManager.Instance.TranslateFullName(
                            originalName, scope, tmpHandlers.ToArray())));
            }

            TranslationHelper.Instance.StartCoroutine(
                _pathTracker.TrackTranslationCoroutine(TranslationCoroutine, scope, path, tmpCallbacks));

            yield return WhileNotDone();
        }
    }
}
