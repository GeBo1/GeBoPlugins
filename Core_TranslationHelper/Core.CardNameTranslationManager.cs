using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using UnityEngine;

#if AI
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    internal class CardNameTranslationManager
    {
        private static ManualLogSource _Logger;

        internal static readonly Dictionary<string, string> recentTranslationsByName = new Dictionary<string, string>();
        internal static readonly HashSet<string> noTranslate = new HashSet<string>();

        internal static readonly KeyValuePair<string, string>[] Suffixes =
        {
            new KeyValuePair<string, string>("君", "kun"), new KeyValuePair<string, string>("ちゃん", "chan")
        };

        private static readonly Regex ContainsJapaneseCharRegex = new Regex(
            @"([\u3000-\u303F]|[\u3040-\u309F]|[\u30A0-\u30FF]|[\uFF00-\uFFEF]|[\u4E00-\u9FAF]|[\u2605-\u2606]|[\u2190-\u2195]|\u203B)",
            Constants.SupportedRegexCompilationOption);

        private static readonly char[] SpaceSplitter = {' '};
        private readonly Dictionary<string, List<TranslationResultHandler>> _nameTracker;

        private readonly HashSet<string> cardsInProgress;

        private readonly IEnumerable<string> SuffixFormats = new[] {"[{0}]", "-{0}", "{0}"};

        internal CardNameTranslationManager()
        {
            _nameTracker = new Dictionary<string, List<TranslationResultHandler>>();
            cardsInProgress = new HashSet<string>();
        }

        internal static ManualLogSource Logger => _Logger ?? (_Logger = TranslationHelper.Logger);

        internal static CardNameTranslationManager Instance => TranslationHelper.CardNameManager;

        internal NameTranslator NameTranslator => TranslationHelper.Instance.NameTranslator;

        public IEnumerator WaitOnCardTranslations()
        {
            while (cardsInProgress.Count > 0)
            {
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitWhile(() => cardsInProgress.Count > 0);
        }

        public bool CardNeedsTranslation(ChaFile file)
        {
            return file.EnumerateNames().Select(n => n.Value)
                .Any(n => !noTranslate.Contains(n) && ContainsJapaneseCharRegex.IsMatch(n));
        }

        public IEnumerator WaitOnCard(ChaFile file)
        {
            var regId = file.GetRegistrationID();
            while (cardsInProgress.Contains(regId))
            {
                yield return new WaitForSeconds(1f);
            }

            yield return new WaitWhile(() => cardsInProgress.Contains(regId));
            //Logger.LogDebug($"TranslateCardNames: {file}: card done: {regId}");
        }

        public IEnumerator TranslateCardNames(ChaFile file)
        {
            var regId = file.GetRegistrationID();
            if (!cardsInProgress.Contains(regId))
            {
                cardsInProgress.Add(regId);
                Logger.LogDebug($"TranslateCardNames: {file}: start");
                if (CardNeedsTranslation(file))
                {
                    Logger.LogDebug($"TranslateCardNames: {file}: adding new card: {regId}");
                    /*yield return */
                    TranslationHelper.Instance.StartCoroutine(new CardJob(file, CardComplete).Start());
                }
                else
                {
                    cardsInProgress.Remove(regId);
                }

                yield return null;

#if false
                //float cutoff = 0;
                while (cardsInProgress.Contains(regId))
                {
                    //Logger.LogDebug($"TranslateCardNames: {file}: waiting on card: {regId}");
                    yield return new WaitForSecondsRealtime(1f);

                    if (Time.time > cutoff)
                    {
                        //Logger.LogDebug($"TranslateCardNames: {file}: waiting on card: {regId}");
                        cutoff
 = Time.time + 60f;
                    }
                    yield return null;
            }
#endif
            }
        }

        private void CardComplete(ChaFile chaFile)
        {
            var regId = chaFile.GetRegistrationID();
            //Logger.LogDebug($"CardComplete: {regId}");
            cardsInProgress.Remove(regId);
        }

        public IEnumerator TranslateCardName(string originalName, byte gender, TranslationResultHandler callback)
        {
            if (recentTranslationsByName.TryGetValue(originalName, out var cachedName))
            {
                //Logger.LogDebug($"TranslateCardName: {originalName}: cache hit: {cachedName}");
                callback(new SimpleTranslationResult(true, cachedName));
            }

            //Logger.LogDebug($"TranslateCardName: {originalName}: start");
            var newRequest = false;
            if (!_nameTracker.TryGetValue(originalName, out var actions))
            {
                //Logger.LogDebug($"TranslateCardName: {originalName}: add to tracker");
                actions = _nameTracker[originalName] = new List<TranslationResultHandler>();
                newRequest = true;
            }

            //Logger.LogDebug($"TranslateCardName: {originalName}: add callback {callback}");
            actions.Add(callback);
            if (newRequest)
            {
                //Logger.LogDebug($"TranslateCardName: {originalName}: start new request");
                yield return TranslationHelper.Instance.StartCoroutine(new NameJob(originalName, gender,
                    TranslationHelper.SplitNamesBeforeTranslate, CardNameComplete).Start());
            }

            yield return new WaitWhile(() => _nameTracker.ContainsKey(originalName));
            /*
            float cutoff = 0;

            while (nameTracker.ContainsKey(originalName))
            {
                if (Time.time > cutoff)
                {
                    //Logger.LogDebug($"waiting on: {originalName}");
                    cutoff = Time.time + 60;
                }
                yield return null;
            }
            */
        }

        private void CardNameComplete(KeyValuePair<string, string> names)
        {
            //Logger.LogDebug($"CardNameComplete: {names.Key}, {names.Value}");

            if (!names.Value.IsNullOrEmpty())
            {
                // store result in cache
                if (names.Key == names.Value)
                {
                    noTranslate.Add(names.Value);
                }
                else
                {
                    recentTranslationsByName[names.Key] = names.Value;
                }
            }

            if (_nameTracker.TryGetValue(names.Key, out var actions))
            {
                _nameTracker.Remove(names.Key);
                //Logger.LogDebug($"Processing {actions}");
                var result = new SimpleTranslationResult(true, names.Value);
                foreach (var action in actions.ToArray())
                {
                    //Logger.LogDebug($"Invoking {action}");
                    action(result);
                }
            }
        }

        private IEnumerator TranslateName(string originalName, byte gender,
            TranslationResultHandler callback)
        {
            var cardTranslationMode = TranslationHelper.Instance.CurrentCardLoadTranslationMode;
            var suffixedName = originalName;

            var activeSuffix = new KeyValuePair<string, string>(string.Empty, string.Empty);

            if (cardTranslationMode == CardLoadTranslationMode.Disabled ||
                !GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(suffixedName))
            {
                //Logger.LogDebug($"TranslateName: nothing to do: {originalName}");
                callback(new SimpleTranslationResult(false, originalName, "Disabled or already translated"));
                yield break;
            }


            if (cardTranslationMode >= CardLoadTranslationMode.CacheOnly)
            {
                //Logger.LogDebug($"TranslateName: attempting translation (cached): {originalName}");
                if (NameTranslator.TryTranslateName(originalName, out var translatedName))
                {
                    translatedName = CleanTranslationResult(translatedName, string.Empty);
                    if (originalName != translatedName)
                    {
                        //Logger.LogInfo($"TranslateName: Translated card name (cached): {originalName} -> {translatedName}");
                        callback(new SimpleTranslationResult(true, translatedName));
                        yield break;
                    }
                }

                if (TranslationHelper.TranslateNameWithSuffix.Value)
                {
                    activeSuffix = Suffixes[gender];
                    suffixedName = string.Concat(originalName, activeSuffix.Key);
                    //Logger.LogDebug($"TranslateName: attempting translation (cached): {suffixedName}");
                    if (NameTranslator.TryTranslateName(suffixedName, out var translatedName2))
                    {
                        translatedName2 = CleanTranslationResult(translatedName2, activeSuffix.Value);
                        if (suffixedName != translatedName2)
                        {
                            //Logger.LogInfo($"TranslateName: Translated card name (cached/suffixed): {originalName} -> {translatedName2}");
                            callback(new SimpleTranslationResult(true, translatedName2));
                            yield break;
                        }
                    }
                }
            }


            if (cardTranslationMode == CardLoadTranslationMode.FullyEnabled)
            {
                // suffixedName will be origName if TranslateNameWithSuffix is off, so just use it here
                //Logger.LogDebug($"TranslateName: attempting translation (async): {suffixedName}");
                NameTranslator.TranslateNameAsync(suffixedName, result =>
                {
                    if (result.Succeeded)
                    {
                        var translatedName = CleanTranslationResult(result.TranslatedText, activeSuffix.Value);
                        if (suffixedName != translatedName)
                        {
                            //Logger.LogInfo($"TranslateName: Translated card name (async): {originalName} -> {translatedName}");
                            callback(new SimpleTranslationResult(true, translatedName));
                            return;
                        }
                    }

                    callback(result);
                });
                yield return null;
            }
            else
            {
                callback(new SimpleTranslationResult(false, originalName, "Unable to translate name"));
            }
        }

        private string CleanTranslationResult(string input, string suffix)
        {
            return string.Join(" ",
                input.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => CleanTranslationResultSection(s, suffix)).ToArray()).Trim();
        }

        private string CleanTranslationResultSection(string input, string suffix)
        {
            var output = input;
            var preClean = string.Empty;
            //Logger.LogDebug($"Cleaning: '{input}'");
            while (preClean != output)
            {
                preClean = output;
                if (!suffix.IsNullOrEmpty())
                {
                    foreach (var format in SuffixFormats)
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

                output = output.Trim().Trim(TranslationHelper.NameTrimChars.Value.ToCharArray()).Trim();
                //Logger.LogDebug($"Cleaning: '{input}': trimmed: => '{output}");
            }

            //Logger.LogDebug($"Cleaning: '{input}': result: '{output}'");
            return output;
        }

        internal abstract class BaseJob<T>
        {
            private readonly Action<T> Callback;
            private int jobCount;

            internal BaseJob(Action<T> callback)
            {
                Callback = callback;
                Complete = false;
            }

            internal bool Complete { get; private set; }

            protected abstract IEnumerator StartJobs();
            protected abstract T GetResult();

            internal IEnumerator Start()
            {
                //Logger.LogDebug($"Start: {this}");
                yield return TranslationHelper.Instance.StartCoroutine(Execute());
            }

            private IEnumerator Execute()
            {
                //Logger.LogDebug($"Start: {this}");
                yield return TranslationHelper.Instance.StartCoroutine(StartJobs());
                while (jobCount > 0)
                {
                    yield return null;
                }

                OnComplete();
            }

            protected virtual void OnComplete()
            {
                //Logger.LogDebug($"OnComplete: {this}");
                Complete = true;
                Callback(GetResult());
            }

            internal void JobStarted()
            {
                //Logger.LogDebug($"Starting job: {this}");
                Interlocked.Increment(ref jobCount);
            }

            internal void JobComplete()
            {
                Interlocked.Decrement(ref jobCount);
                //Logger.LogDebug($"Finished job: {this}");
            }
        }

        internal class NameJob : BaseJob<KeyValuePair<string, string>>
        {
            internal NameJob(string originalName, byte gender, bool splitNames,
                Action<KeyValuePair<string, string>> callback) : base(callback)
            {
                OriginalName = originalName;
                Gender = gender;
                SplitNames = splitNames;
            }

            internal string OriginalName { get; }
            internal string TranslatedName { get; private set; }
            internal byte Gender { get; }
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
                    var job = TranslationHelper.Instance.StartCoroutine(Instance.TranslateName(namePart.Value, Gender,
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
                TranslatedName = string.Join(" ", NameParts.ToArray()).Trim();
                base.OnComplete();
            }
        }

        internal class CardJob : BaseJob<ChaFile>
        {
            private readonly ChaFile ChaFile;

            internal CardJob(ChaFile chaFile, Action<ChaFile> callback) : base(callback)
            {
                ChaFile = chaFile;
                FullNameHandled = false;
            }

            public bool FullNameHandled { get; private set; }

            protected override ChaFile GetResult()
            {
                return ChaFile;
            }

            protected override IEnumerator StartJobs()
            {
                //Logger.LogDebug($"CardJob.StartJobs: {this}");
                var jobs = new List<Coroutine>();

                var fullName = ChaFile.GetFullName();

                foreach (var name in ChaFile.EnumerateNames())
                {
                    FullNameHandled = FullNameHandled || name.Value == fullName;
                    //Logger.LogDebug($"CardJob.StartJobs: {this}: {name.Key}, {name.Value}");
                    var i = name.Key;
                    var chaFile = ChaFile;

                    if (noTranslate.Contains(name.Value))
                    {
                        continue;
                    }

                    if (recentTranslationsByName.TryGetValue(name.Value, out var cachedName))
                    {
                        //Logger.LogDebug($"StartJobs: {this}: cache hit: {name.Value} => {cachedName}");
                        chaFile.SetName(i, cachedName);
                        continue;
                    }

                    if (!ContainsJapaneseCharRegex.IsMatch(name.Value))
                    {
                        noTranslate.Add(name.Value);
                        continue;
                    }

                    JobStarted();

                    var job = TranslationHelper.Instance.StartCoroutine(Instance.TranslateCardName(name.Value,
                        ChaFile.parameter.sex, result =>
                        {
                            if (result.Succeeded && name.Value != result.TranslatedText)
                            {
                                chaFile.SetName(i, result.TranslatedText);
                                if (TranslationHelper.Instance.CurrentCardLoadTranslationMode >
                                    CardLoadTranslationMode.CacheOnly)
                                {
                                    TranslationHelper.Instance.AddTranslatedNameToCache(name.Value,
                                        result.TranslatedText);
                                }
                            }

                            JobComplete();
                        }));

                    jobs.Add(job);
                }

                foreach (var job in jobs)
                {
                    //Logger.LogDebug($"CardJob.StartJobs: yield {job}");
                    yield return job;
                }
            }
        }
    }
}
