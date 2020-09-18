using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.XPath;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Translation;
using TranslationHelperPlugin.Utils;
using UnityEngine;
#if AI || HS2
using AIChara;

#endif

namespace TranslationHelperPlugin
{
    internal class CardNameTranslationManager
    {
        private static ManualLogSource _logger;
        internal static readonly NameScopeDictionary<Dictionary<string, string>> RecentTranslationsByName = new NameScopeDictionary<Dictionary<string, string>>();
        internal static readonly NameScopeDictionary<HashSet<string>> NoTranslate = new NameScopeDictionary<HashSet<string>>();

        internal static readonly KeyValuePair<string, string>[] Suffixes =
        {
            new KeyValuePair<string, string>("君", "kun"), new KeyValuePair<string, string>("ちゃん", "chan")
        };

        private static readonly Limiter TranslateNameLimiter = new Limiter(100, nameof(TranslateNameLimiter), true);

        private readonly HashSet<string> _cardsInProgress;
        private readonly NameTracker _nameTracker;

        private readonly IEnumerable<string> _suffixFormats = new[] {"[{0}]", "-{0}", "{0}"};

        internal CardNameTranslationManager()
        {
            _nameTracker = new NameTracker();
            _cardsInProgress = new HashSet<string>();
            _waitWhileCardsAreInProgress = new WaitWhile(AreCardsInProgress);
        }


        private static char[] SpaceSplitter => TranslationHelper.SpaceSplitter;

        internal static ManualLogSource Logger => _logger ?? (_logger = TranslationHelper.Logger);

        internal static CardNameTranslationManager Instance => TranslationHelper.CardNameManager;

        internal NameTranslator NameTranslator => TranslationHelper.Instance.NameTranslator;

        internal static bool CanForceSplitNameString(string nameString)
        {
            return nameString.Split(TranslationHelper.SpaceSplitter, StringSplitOptions.RemoveEmptyEntries).Length == 2;
        }

        public bool AreCardsInProgress() => _cardsInProgress.Count > 0;

        private readonly IEnumerator _waitWhileCardsAreInProgress;
        public IEnumerator WaitOnCardTranslations()
        {
            yield return _waitWhileCardsAreInProgress;
        }

        public bool CardNeedsTranslation(ChaFile file)
        {
            var sex = file.GetSex();
            return file.EnumerateNames().Any(name =>
                StringUtils.ContainsJapaneseChar(name.Value) &&
                !NoTranslate[new NameScope(sex, file.GetNameType(name.Key))].Contains(name.Value));
        }

        public IEnumerator WaitOnCard(ChaFile file)
        {
            if (file == null) yield break;
            var regId = file.GetRegistrationID();

            bool NotDone() => _cardsInProgress.Contains(regId);
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
                if (NoTranslate[scope].Contains(entry.Value)) continue;
                if (RecentTranslationsByName[scope].TryGetValue(entry.Value, out var translatedName))
                {
                    file.SetName(entry.Key, translatedName);
                }
            }
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
                if (done) yield break;
                yield return null;
            }
            
            if (RecentTranslationsByName[scope].TryGetValue(originalName, out var cachedName))
            {
                tmpCallbacks.CallHandlers(new SimpleTranslationResult(true, cachedName));
                yield break;
            }

           

            if (!_nameTracker.TryAddHandlers(scope, originalName, tmpCallbacks))
            {
                _nameTracker.TrackName(scope, originalName, tmpCallbacks);
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
            var enabled = TranslationHelper.Instance.CurrentCardLoadTranslationMode != CardLoadTranslationMode.Disabled;
            if (!names.Value.IsNullOrEmpty())
            {
                // store result in cache
                if (names.Key == names.Value)
                {
                    if (enabled) NoTranslate[scope].Add(names.Value);
                }
                else
                {
                    NoTranslate[scope].Remove(names.Key);
                    RecentTranslationsByName[scope][names.Key] = names.Value;
                }
            }

            var result = new SimpleTranslationResult(names.Value != null && names.Value != names.Key, names.Value);
            _nameTracker.CallHandlers(scope, names.Key, result);
        }

        private IEnumerator TranslateName(string originalName, NameScope nameScope,
            TranslationResultHandler callback)
        {
            Logger.DebugLogDebug($"TranslateName(\"{originalName}\", {nameScope}, {callback})");
            yield return TranslateNameLimiter.Start();

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
                CallbackWrapper(new SimpleTranslationResult(false, originalName, "Disabled or already translated"));
                yield break;
            }


            if (cardTranslationMode >= CardLoadTranslationMode.CacheOnly)
            {
                Logger.DebugLogDebug($"TranslateName: attempting translation (XUA cached): {originalName}");
                if (NameTranslator.TryTranslateName(originalName, nameScope, out var translatedName))
                {
                    translatedName = CleanTranslationResult(translatedName, string.Empty);
                    if (originalName != translatedName)
                    {
                        Logger.DebugLogDebug($"TranslateName: Translated card name (XUA cached): {originalName} -> {translatedName}");
                        CallbackWrapper(new SimpleTranslationResult(true, translatedName));
                        yield break;
                    }
                }

                yield return null;

                if (TranslationHelper.TranslateNameWithSuffix.Value && nameScope.Sex != CharacterSex.Unspecified)
                {
                    activeSuffix = Suffixes[(int)nameScope.Sex];
                    suffixedName = string.Concat(originalName, activeSuffix.Key);
                    Logger.DebugLogDebug($"TranslateName: attempting translation (XUA cached/suffixed): {suffixedName}");
                    if (NameTranslator.TryTranslateName(suffixedName, nameScope, out var translatedName2))
                    {
                        translatedName2 = CleanTranslationResult(translatedName2, activeSuffix.Value);
                        if (suffixedName != translatedName2)
                        {
                            Logger.DebugLogDebug($"TranslateName: Translated card name (XUA cached/suffixed): {originalName} -> {translatedName2}");
                            CallbackWrapper(new SimpleTranslationResult(true, translatedName2));
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
                        if (suffixedName != translatedName)
                        {
                            Logger.DebugLogDebug($"TranslateName: Translated card name (XUA async): {originalName} -> {translatedName}");
                            CallbackWrapper(new SimpleTranslationResult(true, translatedName));
                            return;
                        }
                    }

                    CallbackWrapper(result);
                });
            }
            else
            {
                CallbackWrapper(new SimpleTranslationResult(false, originalName, "Unable to translate name"));
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
                        if (output.TrimEnd().EndsWith(testSuffix))
                        {
                            //Logger.LogDebug($"Cleaning: '{input}': suffix match: {testSuffix} <= '{output}'");
                            output = output.Remove(output.LastIndexOf(testSuffix, StringComparison.InvariantCulture))
                                .TrimEnd();
                            //Logger.LogDebug($"Cleaning: '{input}': suffix match: {testSuffix} => '{output}'");
                            break;
                        }
                    }
                }

                output = output.Trim(toRemove).Trim();
                //Logger.LogDebug($"Cleaning: '{input}': trimmed: => '{output}");
            }

            //Logger.LogDebug($"Cleaning: '{input}': result: '{output}'");
            return output;
        }

        public static void ClearCaches()
        {

            NoTranslate.Clear();
            RecentTranslationsByName.Clear();
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
                //Logger.LogDebug($"Start: {this}");
                yield return StartCoroutine(Execute());
            }

            protected IEnumerator Execute()
            {
                //Logger.LogDebug($"Start: {this}");
                yield return StartCoroutine(StartJobs());
                while (Interlocked.Read(ref _jobCount) > 0)
                {
                    yield return null;
                }

                OnComplete();
            }

            protected virtual void OnComplete()
            {
                //Logger.LogDebug($"OnComplete: {this}");
                Complete = true;
                _callback(GetResult());
            }

            internal virtual void JobStarted()
            {
                //Logger.LogDebug($"Starting job: {this}");
                Interlocked.Increment(ref _jobCount);
            }

            internal virtual void JobComplete()
            {
                Interlocked.Decrement(ref _jobCount);
                //Logger.LogDebug($"Finished job: {this}");
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

            internal NameScope NameScope { get; }
            internal string OriginalName { get; }
            internal string TranslatedName { get; private set; }

            internal int TranslationScope => NameScope?.TranslationScope ?? NameScope.BaseScope;
            internal CharacterSex Sex => NameScope?.Sex ?? CharacterSex.Unspecified;

            internal NameType NameType => NameScope?.NameType ?? NameType.Unclassified;
            private bool SplitNames { get; }

            private List<string> NameParts { get; set; }

            protected override KeyValuePair<string, string> GetResult()
            {
                return new KeyValuePair<string, string>(OriginalName, TranslatedName);
            }

            protected override IEnumerator StartJobs()
            {
                //Logger.LogDebug($"NameJob.StartJobs: {this}");
                NameParts = new List<string>(new[] {OriginalName});

                if (SplitNames)
                {
                    NameParts = new List<string>(OriginalName.Split(TranslationHelper.SpaceSplitter,
                        StringSplitOptions.RemoveEmptyEntries));
                }

                var jobs = new List<Coroutine>();
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
                    yield return job;
                }
            }

            protected override void OnComplete()
            {
                TranslatedName = string.Join(TranslationHelper.SpaceJoiner, NameParts.ToArray()).Trim();
                base.OnComplete();
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

            internal void JobCompleteHandler(ITranslationResult _)
            {
                if (!FullNameHandled)
                {
                    var orig = _chaFile.GetOriginalFullName();
                    var current = _chaFile.GetFullName();
                    TranslationHelper.Instance.AddTranslatedNameToCache(orig, current, true);
                }

                JobComplete();
            }

            protected override IEnumerator StartJobs()
            {
                Logger.DebugLogDebug($"CardJob.StartJobs: {this}");
                var jobs = new List<Coroutine>();

                var fullName = _chaFile.GetFullName();

                foreach (var name in _chaFile.EnumerateNames())
                {
                    FullNameHandled = FullNameHandled || name.Value == fullName;

                    var i = name.Key;
                    var chaFile = _chaFile;

                    var nameScope = new NameScope(_chaFile.GetSex(), _chaFile.GetNameType(i));

                    if (NoTranslate[nameScope].Contains(name.Value))
                    {
                        continue;
                    }

                    if (RecentTranslationsByName[nameScope].TryGetValue(name.Value, out var cachedName))
                    {
                        //Logger.LogDebug($"StartJobs: {this}: cache hit: {name.Value} => {cachedName}");
                        chaFile.SetTranslatedName(i, cachedName);
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
                        Handlers.UpdateCardName(_chaFile, i),
                        Handlers.AddNameToCache(name.Value, true),
                        JobCompleteHandler));

                    jobs.Add(job);
                }

                foreach (var job in jobs)
                {
                    yield return job;
                }
            }
        }
    }
}
