using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Translation;
using TranslationHelperPlugin.Utils;
using UnityEngine;
using Handlers = TranslationHelperPlugin.Chara.Handlers;
#if AI || HS2
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    internal class CardNameTranslationManager
    {
        private static readonly CacheFunctionHelper CacheRecentTranslationsHelper = new CacheFunctionHelper();

        internal static readonly NameScopeDictionary<Dictionary<string, string>> RecentTranslationsByName =
            new NameScopeDictionary<Dictionary<string, string>>(TranslationHelper.StringCacheInitializer);

        internal static readonly NameScopeDictionary<HashSet<string>> NoTranslate =
            new NameScopeDictionary<HashSet<string>>(() => new HashSet<string>(TranslationHelper.NameStringComparer));

        internal static readonly KeyValuePair<string, string>[] Suffixes =
        {
            new KeyValuePair<string, string>("君", "kun"), new KeyValuePair<string, string>("ちゃん", "chan")
        };

        private static readonly CoroutineLimiter TranslateNameLimiter =
            new CoroutineLimiter(100, nameof(TranslateNameLimiter), true);

        private readonly HashSet<string> _cardsInProgress;
        private readonly TranslationTracker _nameTracker;

        private readonly IEnumerable<string> _suffixFormats = new[] {"[{0}]", "-{0}", "{0}"};

        private readonly IEnumerator _waitWhileCardsAreInProgress;

        internal CardNameTranslationManager()
        {
            _nameTracker = new TranslationTracker($"{nameof(CardNameTranslationManager)}.{nameof(_nameTracker)}");
            _cardsInProgress = new HashSet<string>();
            _waitWhileCardsAreInProgress = new WaitWhile(AreCardsInProgress);
            TranslationHelper.CardTranslationBehaviorChanged += CardTranslationHelperBehaviorChanged;
        }

        private static char[] SpaceSplitter => TranslationHelper.SpaceSplitter;

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal static CardNameTranslationManager Instance => TranslationHelper.CardNameManager;

        internal NameTranslator NameTranslator => TranslationHelper.Instance.NameTranslator;

        private void CardTranslationHelperBehaviorChanged(object sender, EventArgs e)
        {
            ClearCaches();
        }

        internal static bool CanForceSplitNameString(string nameString)
        {
            return nameString.Split(TranslationHelper.SpaceSplitter, StringSplitOptions.RemoveEmptyEntries).Length == 2;
        }

        public bool AreCardsInProgress()
        {
            return _cardsInProgress.Count > 0;
        }

        public IEnumerator WaitOnCardTranslations()
        {
            yield return _waitWhileCardsAreInProgress;
        }

        public bool CardNeedsTranslation(ChaFile file)
        {
            return file.EnumerateNames().Any(name => StringUtils.ContainsJapaneseChar(name.Value));
        }

        public IEnumerator WaitOnCard(ChaFile file)
        {
            if (file == null) yield break;
            var regId = file.GetRegistrationID();

            bool NotDone()
            {
                return _cardsInProgress.Contains(regId);
            }

            // delay once if card does not appear active yet
            if (!NotDone()) yield return null;

            yield return new WaitWhile(NotDone);

            ApplyTranslations(file);
            Logger.DebugLogDebug($"TranslateCardNames: {file}: card done: {regId}");
        }

        private void ApplyTranslations(ChaFile file)
        {
            if (file.TryGetTranslationHelperController(out var controller))
            {
                controller.ApplyTranslations();
                return;
            }

            var sex = file.GetSex();
            // partially loaded, no controller available
            foreach (var entry in file.EnumerateNames())
            {
                var scope = new NameScope(sex, file.GetNameType(entry.Key));
                if (TryGetRecentTranslation(scope, entry.Value, out var translatedName) &&
                    !TranslationHelper.NameStringComparer.Equals(translatedName, entry.Value))
                {
                    file.SetName(entry.Key, translatedName);
                }
            }
        }

        public static bool TryGetRecentTranslation(NameScope scope, string name, out string result)
        {
            if (RecentTranslationsByName[scope].TryGetValue(name, out result))
            {
                Logger.DebugLogDebug($"TryGetRecentTranslation({scope}, {name}): hit => {result}");
                return true;
            }

            if (!NoTranslate[scope].Contains(name))
            {
                Logger.DebugLogDebug($"TryGetRecentTranslation({scope}, {name}): miss");
                return false;
            }

            // found in NoTranslate, return original name
            Logger.DebugLogDebug($"TryGetRecentTranslation({scope}, {name}): no-translate");
            result = name;
            return true;
        }

        public static void CacheRecentTranslation(NameScope scope, string originalName, string translatedName)
        {
            // ReSharper disable once RedundantAssignment - used in DEBUG
            var start = Time.realtimeSinceStartup;
            var key = new {scope.TranslationScope, originalName, translatedName};

            try
            {
                if (CacheRecentTranslationsHelper.WasCalledRecently(key)) return;
                if (translatedName.IsNullOrEmpty() || originalName.IsNullOrEmpty()) return;
                if (TranslationHelper.NameStringComparer.Equals(originalName, translatedName))
                {
                    if (TranslationHelper.Instance.CurrentCardLoadTranslationMode > CardLoadTranslationMode.Disabled &&
                        !StringUtils.ContainsJapaneseChar(translatedName))
                    {
                        Logger.DebugLogDebug(
                            $"{nameof(CacheRecentTranslation)}({scope}, {originalName}, {translatedName}): NoTranslate");
                        NoTranslate[scope].Add(translatedName);
                    }
                }
                else
                {
                    NoTranslate[scope].Remove(originalName);
                    NoTranslate[scope].Add(translatedName);
                    RecentTranslationsByName[scope][originalName] = translatedName;
                    Logger.DebugLogDebug(
                        $"{nameof(CacheRecentTranslation)}({scope}, {originalName}, {translatedName}): Caching");
                }

                CacheRecentTranslationsHelper.WasCalledRecently(key);
            }
            finally
            {
                Logger.DebugLogDebug(
                    $"CardNameTranslationManager.CacheRecentTranslation(path): {Time.realtimeSinceStartup - start:000.0000000000}");
            }
        }

        internal static bool NameNeedsTranslation(string name, NameScope scope)
        {
            return !name.IsNullOrWhiteSpace() &&
                   !NoTranslate[scope].Contains(name) &&
                   StringUtils.ContainsJapaneseChar(name);
        }

        public IEnumerator TranslateCardNames(ChaFile file)
        {
            var regId = file.GetRegistrationID();
            // if it's in progress and the controller is available then
            // the outstanding job will handle things
            if (_cardsInProgress.Contains(regId)) yield break;

            _cardsInProgress.Add(regId);

            if (CardNeedsTranslation(file))
            {
                file.StartMonitoredCoroutine(new CardJob(file, CardComplete).Start());
            }
            else
            {
                CardComplete(file);
            }
        }

        private void CardComplete(ChaFile chaFile)
        {
            var regId = chaFile.GetRegistrationID();
            _cardsInProgress.Remove(regId);
        }

        public IEnumerator TranslateFullName(string originalName, NameScope scope,
            params TranslationResultHandler[] callbacks)
        {
            if (TranslationHelper.TryFastTranslateFullName(scope, originalName, out var fastName))
            {
                callbacks.CallHandlers(new TranslationResult(originalName, fastName));
                yield break;
            }

            yield return TranslationHelper.Instance.StartCoroutine(
                TranslateCardName(originalName, scope, CanForceSplitNameString(originalName), callbacks));
        }


        public IEnumerator TranslateCardName(string originalName, NameScope scope,
            params TranslationResultHandler[] callbacks)
        {
            return TranslateCardName(originalName, scope, false, callbacks);
        }


        public IEnumerator TranslateCardName(string originalName, NameScope scope, bool forceSplit,
            params TranslationResultHandler[] callbacks)
        {
            var done = false;
            var tmpCallbacks = callbacks.ToList();
            tmpCallbacks.Add(r => done = true);

            IEnumerator WhileNotDone()
            {
                while (true)
                {
                    if (done) yield break;
                    yield return null;
                }
            }

            if (TryGetRecentTranslation(scope, originalName, out var cachedName))
            {
                tmpCallbacks.CallHandlers(new TranslationResult(originalName, cachedName));
                yield break;
            }


            if (!_nameTracker.TryAddHandlers(scope, originalName, tmpCallbacks))
            {
                _nameTracker.TrackKey(scope, originalName, tmpCallbacks);
                yield return null; // improves batching of handlers

                void Handler(KeyValuePair<string, string> names)
                {
                    CardNameComplete(names, scope);
                }

                /*yield return*/
                TranslationHelper.Instance.StartCoroutine(new NameJob(originalName, scope,
                    TranslationHelper.SplitNamesBeforeTranslate || forceSplit, Handler).Start());
            }

            yield return WhileNotDone();
        }

        private void CardNameComplete(KeyValuePair<string, string> names, NameScope scope = null)
        {
            Logger.DebugLogDebug($"CardNameComplete: {names.Key}, {names.Value}");
            if (scope == null) scope = new NameScope();
            CacheRecentTranslation(scope, names.Key, names.Value);
            var result = new TranslationResult(names.Key, names.Value);
            Logger.DebugLogDebug($"CardNameComplete: _nameTracker.CallHandlers({scope}, {names.Key}, {result})");
            _nameTracker.CallHandlers(scope, names.Key, result);
        }

        private IEnumerator TranslateName(string originalName, NameScope nameScope,
            TranslationResultHandler callback)
        {
            Logger.DebugLogDebug($"TranslateName(\"{originalName}\", {nameScope}, {callback})");
            yield return TranslationHelper.Instance.StartCoroutine(TranslateNameLimiter.Start());

            void CallbackWrapper(ITranslationResult translationResult)
            {
                TranslateNameLimiter.EndImmediately();
                callback(translationResult);
            }

            var cardTranslationMode = TranslationHelper.Instance.CurrentCardLoadTranslationMode;
            var suffixedName = originalName;

            var activeSuffix = new KeyValuePair<string, string>(string.Empty, string.Empty);

            if (cardTranslationMode == CardLoadTranslationMode.Disabled ||
                !GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(suffixedName))
            {
                Logger.DebugLogDebug($"TranslateName: nothing to do: {originalName}");
                CallbackWrapper(new TranslationResult(false, originalName, "Disabled or already translated"));
                yield break;
            }


            if (cardTranslationMode >= CardLoadTranslationMode.CacheOnly)
            {
                Logger.DebugLogDebug($"TranslateName: attempting translation (XUA cached): {originalName}");
                if (NameTranslator.TryTranslateName(originalName, nameScope, out var translatedName))
                {
                    translatedName = CleanTranslationResult(translatedName, string.Empty);
                    if (!TranslationHelper.NameStringComparer.Equals(originalName, translatedName))
                    {
                        Logger.DebugLogDebug(
                            $"TranslateName: Translated card name (XUA cached): {originalName} -> {translatedName}");
                        CallbackWrapper(new TranslationResult(true, translatedName));
                        yield break;
                    }
                }

                yield return null;

                if (TranslationHelper.TranslateNameWithSuffix.Value && nameScope.Sex != CharacterSex.Unspecified)
                {
                    activeSuffix = Suffixes[(int)nameScope.Sex];
                    suffixedName = string.Concat(originalName, activeSuffix.Key);
                    Logger.DebugLogDebug(
                        $"TranslateName: attempting translation (XUA cached/suffixed): {suffixedName}");
                    if (NameTranslator.TryTranslateName(suffixedName, nameScope, out translatedName))
                    {
                        translatedName = CleanTranslationResult(translatedName, activeSuffix.Value);
                        if (!TranslationHelper.NameStringComparer.Equals(suffixedName, translatedName))
                        {
                            Logger.DebugLogDebug(
                                $"TranslateName: Translated card name (XUA cached/suffixed): {originalName} -> {translatedName}");
                            CallbackWrapper(new TranslationResult(true, translatedName));
                            yield break;
                        }
                    }

                    yield return null;
                }
            }


            if (cardTranslationMode == CardLoadTranslationMode.FullyEnabled)
            {
                // suffixedName will be origName if TranslateNameWithSuffix is off, so just use it here
                Logger.DebugLogDebug($"TranslateName: attempting translation (XUA async): {suffixedName}");
                NameTranslator.TranslateNameAsync(suffixedName, nameScope, result =>
                {
                    if (result.Succeeded)
                    {
                        var translatedName = CleanTranslationResult(result.TranslatedText, activeSuffix.Value);
                        if (!TranslationHelper.NameStringComparer.Equals(suffixedName, translatedName))
                        {
                            Logger.DebugLogDebug(
                                $"TranslateName: Translated card name (XUA async): {originalName} -> {translatedName}");
                            CallbackWrapper(new TranslationResult(true, translatedName));
                            return;
                        }
                    }

                    CallbackWrapper(result);
                });
            }
            else
            {
                CallbackWrapper(new TranslationResult(false, originalName, "Unable to translate name"));
            }
        }

        private string CleanTranslationResult(string input, string suffix)
        {
            return string.Join(TranslationHelper.SpaceJoiner,
                input.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => CleanTranslationResultSection(s, suffix)).ToArray()).Trim();
        }

        private string CleanTranslationResultSection(string input, string suffix)
        {
            var output = input;
            var preClean = string.Empty;
            //Logger.LogDebug($"Cleaning: '{input}'");
            var toRemove = TranslationHelper.NameTrimChars.Value.ToCharArray();

            while (preClean != output)
            {
                preClean = output;
                if (!suffix.IsNullOrEmpty())
                {
                    foreach (var format in _suffixFormats)
                    {
                        var testSuffix = string.Format(format, suffix);
                        if (!output.TrimEnd().EndsWith(testSuffix)) continue;
                        //Logger.LogDebug($"Cleaning: '{input}': suffix match: {testSuffix} <= '{output}'");
                        output = output.Remove(output.LastIndexOf(testSuffix, StringComparison.InvariantCulture))
                            .TrimEnd();
                        //Logger.LogDebug($"Cleaning: '{input}': suffix match: {testSuffix} => '{output}'");
                        break;
                    }
                }

                output = output.Trim(toRemove).Trim();
                //Logger.LogDebug($"Cleaning: '{input}': trimmed: => '{output}");
            }

            //Logger.LogDebug($"Cleaning: '{input}': result: '{output}'");
            return output;
        }

        public void ClearCaches()
        {
            CacheRecentTranslationsHelper?.Clear();
            NoTranslate?.Clear();
            RecentTranslationsByName?.Clear();
            _cardsInProgress?.Clear();
        }

        internal abstract class BaseJob<T>
        {
            private readonly Action<T> _callback;
            private long _jobCount;

            internal BaseJob(Action<T> callback)
            {
                _callback = callback;
                Complete = false;
            }

            internal bool Complete { get; private set; }

            protected abstract IEnumerator StartJobs();
            protected abstract T GetResult();

            protected virtual Coroutine StartCoroutine(IEnumerator enumerator)
            {
                return TranslationHelper.Instance.StartCoroutine(enumerator);
            }

            internal IEnumerator Start()
            {
                //Logger.LogDebug($"Start: {this.GetHashCode()}");
                yield return StartCoroutine(Execute());
            }

            protected IEnumerator Execute()
            {
                yield return StartCoroutine(StartJobs());
                while (Interlocked.Read(ref _jobCount) > 0)
                {
                    yield return null;
                }

                OnComplete();
            }

            protected virtual void OnComplete()
            {
                Complete = true;
                _callback(GetResult());
            }

            internal virtual void JobStarted()
            {
                Interlocked.Increment(ref _jobCount);
            }

            internal virtual void JobComplete()
            {
                Interlocked.Decrement(ref _jobCount);
            }
        }

        internal class NameJob : BaseJob<KeyValuePair<string, string>>
        {
            internal NameJob(string originalName, NameScope nameScope, bool splitNames,
                Action<KeyValuePair<string, string>> callback) : base(callback)
            {
                OriginalName = originalName;
                NameScope = nameScope;
                SplitNames = splitNames;
            }

            ~NameJob()
            {
                if (NameParts != null) ListPool<string>.Release(NameParts);
            }

            internal NameScope NameScope { get; }
            internal string OriginalName { get; }
            internal string TranslatedName { get; private set; }

            [UsedImplicitly]
            internal int TranslationScope => NameScope?.TranslationScope ?? NameScope.BaseScope;

            internal CharacterSex Sex => NameScope?.Sex ?? CharacterSex.Unspecified;

            [UsedImplicitly]
            internal NameType NameType => NameScope?.NameType ?? NameType.Unclassified;

            private bool SplitNames { get; }

            private List<string> NameParts { get; set; }

            protected override KeyValuePair<string, string> GetResult()
            {
                return new KeyValuePair<string, string>(OriginalName, TranslatedName);
            }

            protected override IEnumerator StartJobs()
            {
                NameParts = ListPool<string>.Get();
                if (SplitNames)
                {
                    NameParts.AddRange(OriginalName.Split(TranslationHelper.SpaceSplitter,
                        StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    NameParts.Add(OriginalName);
                }

                var jobs = ListPool<Coroutine>.Get();
                try
                {
                    foreach (var namePart in NameParts.Enumerate().ToArray())
                    {
                        var i = namePart.Key;
                        JobStarted();
                        var job = StartCoroutine(Instance.TranslateName(namePart.Value, NameScope,
                            result =>
                            {
                                if (result.Succeeded) NameParts[i] = result.TranslatedText;
                                JobComplete();
                            }));
                        jobs.Add(job);
                    }

                    foreach (var job in jobs)
                    {
                        //Logger.LogDebug($"NameJob.StartJobs: yield {job}");
                        if (job != null) yield return job;
                    }
                }
                finally
                {
                    ListPool<Coroutine>.Release(jobs);
                }

            }

            protected override void OnComplete()
            {
                TranslatedName = string.Join(TranslationHelper.SpaceJoiner, NameParts.ToArray()).Trim();
                CacheRecentTranslation(NameScope, OriginalName, TranslatedName);
                base.OnComplete();
                ListPool<string>.Release(NameParts);
                NameParts = null;
            }

            public override string ToString()
            {
                return $"{this.GetPrettyTypeName()}({OriginalName}, {NameScope}, {SplitNames})";
            }
        }

        internal class CardJob : BaseJob<ChaFile>
        {
            private readonly ChaFile _chaFile;

            internal CardJob(ChaFile chaFile, Action<ChaFile> callback) : base(callback)
            {
                _chaFile = chaFile;
                FullNameHandled = false;
            }

            public bool FullNameHandled { get; private set; }

            protected override Coroutine StartCoroutine(IEnumerator enumerator)
            {
                Controller controller = null;
                _chaFile.SafeProc(cf => cf.GetTranslationHelperController().SafeProcObject(c => controller = c));

                return controller != null
                    ? controller.StartMonitoredCoroutine(enumerator)
                    : base.StartCoroutine(enumerator);
            }

            protected override ChaFile GetResult()
            {
                return _chaFile;
            }

            protected override void OnComplete()
            {
                var orig = _chaFile.GetOriginalFullName();
                var current = _chaFile.GetFullName();
                var scope = new NameScope(_chaFile.GetSex());

                CacheRecentTranslation(scope, orig, current);
                if (orig != current)
                {
                    CharaFileInfoTranslationManager.CacheRecentTranslation(scope, _chaFile.GetFullPath(), current);
                }

                TranslationHelper.Instance.AddTranslatedNameToCache(orig, current,
                    !TranslationHelper.ShowGivenNameFirst);

                if (TranslationHelper.ShowGivenNameFirst)
                {
                    var formattedOrig = _chaFile.GetFormattedOriginalName();
                    CacheRecentTranslation(scope, formattedOrig, current);
                    TranslationHelper.Instance.AddTranslatedNameToCache(formattedOrig, current, true);
                }

                base.OnComplete();
            }

            internal void JobCompleteHandler(ITranslationResult _)
            {
                JobComplete();
            }

            protected override IEnumerator StartJobs()
            {
                var jobs = ListPool<Coroutine>.Get();

                var fullName = _chaFile.GetFullName();

                var handled = HashSetPool<int>.Get();
                if (TranslationHelper.Instance.NamePresetManager.TryTranslateCardNames(_chaFile, out var result))
                {
                    foreach (var entry in result)
                    {
                        FullNameHandled = FullNameHandled || entry.Key == "fullname";
                        var i = GeBoAPI.Instance.ChaFileNameToIndex(entry.Key);
                        if (i < 0 || string.IsNullOrEmpty(entry.Value) || _chaFile.GetName(i) == entry.Value) continue;
                        _chaFile.SetTranslatedName(i, entry.Value);
                        handled.Add(i);
                    }
                }

                var sex = _chaFile.GetSex();
                foreach (var name in _chaFile.EnumerateNames())
                {
                    if (handled.Contains(name.Key)) continue;

                    FullNameHandled = FullNameHandled || name.Value == fullName;

                    var i = name.Key;
                    var chaFile = _chaFile;

                    var nameScope = new NameScope(sex, _chaFile.GetNameType(i));

                    if (TryGetRecentTranslation(nameScope, name.Value, out var cachedName))
                    {
                        // if true, but name unchanged then name in NoTranslate
                        if (name.Value != cachedName)
                        {
                            chaFile.SetTranslatedName(i, cachedName);
                        }

                        continue;
                    }

                    if (!StringUtils.ContainsJapaneseChar(name.Value))
                    {
                        NoTranslate[nameScope].Add(name.Value);
                        continue;
                    }

                    JobStarted();


                    var job = StartCoroutine(Instance.TranslateCardName(name.Value,
                        nameScope,
                        Handlers.UpdateCardName(chaFile, i),
                        Handlers.AddNameToAutoTranslationCache(name.Value, true),
                        JobCompleteHandler));

                    jobs.Add(job);
                }
                HashSetPool<int>.Release(handled);

                foreach (var job in jobs)
                {
                    yield return job;
                }
                ListPool<Coroutine>.Release(jobs);
            }
        }
    }
}
