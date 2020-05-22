using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
#if AI
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    internal class CardNameTranslationManager
    {
        private static ManualLogSource _Logger;

        internal static ManualLogSource Logger => _Logger ?? (_Logger = TranslationHelper.Logger);

        internal static CardNameTranslationManager Instance => TranslationHelper.CardNameManager;

        internal abstract class BaseJob<T>
        {
            internal bool Complete { get; private set; }
            private int jobCount;
            private readonly Action<T> Callback;

            protected abstract IEnumerator StartJobs();
            protected abstract T GetResult();

            internal BaseJob(Action<T> callback)
            {
                Callback = callback;
                Complete = false;
            }

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
            internal string OriginalName { get; }
            internal string TranslatedName { get; private set; }
            internal byte Gender { get; }
            private bool SplitNames { get; }

            private List<string> NameParts { get; set; }

            protected override KeyValuePair<string, string> GetResult()
            {
                return new KeyValuePair<string, string>(OriginalName, TranslatedName);
            }

            internal NameJob(string originalName, byte gender, bool splitNames, Action<KeyValuePair<string, string>> callback) : base(callback)
            {
                OriginalName = originalName;
                Gender = gender;
                SplitNames = splitNames;
            }

            protected override IEnumerator StartJobs()
            {
                //Logger.LogDebug($"NameJob.StartJobs: {this}");
                NameParts = new List<string>(new string[] { OriginalName });

                if (SplitNames)
                {
                    NameParts = new List<string>(OriginalName.Split(TranslationHelper.SpaceSplitter, StringSplitOptions.RemoveEmptyEntries));
                }

                var jobs = new List<Coroutine>();
                foreach (var namePart in NameParts.Enumerate().ToArray())
                {
                    var i = namePart.Key;
                    JobStarted();
                    var job = TranslationHelper.Instance.StartCoroutine(Instance.TranslateName(namePart.Value, Gender, (result) =>
                    {
                        NameParts[i] = result.Value;
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
            public bool FullNameHandled { get; private set; }

            protected override ChaFile GetResult()
            {
                return ChaFile;
            }

            internal CardJob(ChaFile chaFile, Action<ChaFile> callback) : base(callback)
            {
                ChaFile = chaFile;
                FullNameHandled = false;
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

                    var job = TranslationHelper.Instance.StartCoroutine(Instance.TranslateCardName(name.Value, ChaFile.parameter.sex, (result) =>
                    {
                        if (result.Key != result.Value)
                        {
                            chaFile.SetName(i, result.Value);
                            if (TranslationHelper.Instance.CurrentCardLoadTranslationMode > CardLoadTranslationMode.CacheOnly)
                            {
                                TranslationHelper.Instance.AddTranslatedNameToCache(result.Key, result.Value);
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

        private readonly HashSet<string> cardsInProgress;
        private readonly Dictionary<string, List<Action<KeyValuePair<string, string>>>> nameTracker;

        internal static readonly Dictionary<string, string> recentTranslationsByName = new Dictionary<string, string>();
        internal static readonly HashSet<string> noTranslate = new HashSet<string>();

        internal static readonly KeyValuePair<string, string>[] Suffixes = {
            new KeyValuePair<string, string>("君", "kun"),
            new KeyValuePair<string, string>("ちゃん", "chan")
        };

        private readonly IEnumerable<string> SuffixFormats = new string[] { "[{0}]", "-{0}", "{0}" };

        private static readonly Regex ContainsJapaneseCharRegex = new Regex(
            @"([\u3000-\u303F]|[\u3040-\u309F]|[\u30A0-\u30FF]|[\uFF00-\uFFEF]|[\u4E00-\u9FAF]|[\u2605-\u2606]|[\u2190-\u2195]|\u203B)",
            Constants.SupportedRegexCompilationOption);

        private static readonly char[] SpaceSplitter = new char[] { ' ' };

        internal CardNameTranslationManager()
        {
            nameTracker = new Dictionary<string, List<Action<KeyValuePair<string, string>>>>();
            cardsInProgress = new HashSet<string>();
        }

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
            return file.EnumerateNames().Select((n) => n.Value).Any((n) => !noTranslate.Contains(n) && ContainsJapaneseCharRegex.IsMatch(n));
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
                //Logger.LogDebug($"TranslateCardNames: {file}: start");
                if (CardNeedsTranslation(file))
                {
                    //Logger.LogDebug($"TranslateCardNames: {file}: adding new card: {regId}");
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
                        cutoff = Time.time + 60f;
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

        public IEnumerator TranslateCardName(string originalName, byte gender, Action<KeyValuePair<string, string>> callback)
        {
            if (recentTranslationsByName.TryGetValue(originalName, out var cachedName))
            {
                //Logger.LogDebug($"TranslateCardName: {originalName}: cache hit: {cachedName}");
                callback(new KeyValuePair<string, string>(originalName, cachedName));
            }

            //Logger.LogDebug($"TranslateCardName: {originalName}: start");
            var newRequest = false;
            if (!nameTracker.TryGetValue(originalName, out var actions))
            {
                //Logger.LogDebug($"TranslateCardName: {originalName}: add to tracker");
                actions = nameTracker[originalName] = new List<Action<KeyValuePair<string, string>>>();
                newRequest = true;
            }
            //Logger.LogDebug($"TranslateCardName: {originalName}: add callback {callback}");
            actions.Add(callback);
            if (newRequest)
            {
                //Logger.LogDebug($"TranslateCardName: {originalName}: start new request");
                yield return TranslationHelper.Instance.StartCoroutine(new NameJob(originalName, gender, TranslationHelper.SplitNamesBeforeTranslate, CardNameComplete).Start());
            }

            yield return new WaitWhile(() => nameTracker.ContainsKey(originalName));
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

            if (nameTracker.TryGetValue(names.Key, out var actions))
            {
                nameTracker.Remove(names.Key);
                //Logger.LogDebug($"Processing {actions}");
                foreach (var action in actions.ToArray())
                {
                    //Logger.LogDebug($"Invoking {action}");
                    action(names);
                }
            }
        }

        public IEnumerator TranslateName(string originalName, byte gender, Action<KeyValuePair<string, string>> callback)
        {
            var cardTranslationMode = TranslationHelper.Instance.CurrentCardLoadTranslationMode;
            var suffixedName = originalName;

            var done = false;

            var activeSuffix = new KeyValuePair<string, string>(string.Empty, string.Empty);

            if (cardTranslationMode == CardLoadTranslationMode.Disabled || !GeBoAPI.Instance.AutoTranslationHelper.IsTranslatable(suffixedName))
            {
                //Logger.LogDebug($"TranslateName: nothing to do: {originalName}");
                callback(new KeyValuePair<string, string>(originalName, originalName));
                done = true;
            }

            if (!done && cardTranslationMode >= CardLoadTranslationMode.CacheOnly)
            {
                //Logger.LogDebug($"TranslateName: attempting translation (cached): {originalName}");
                if (GeBoAPI.Instance.AutoTranslationHelper.TryTranslate(originalName, out var translatedName))
                {
                    translatedName = CleanTranslationResult(translatedName, string.Empty);
                    if (originalName != translatedName)
                    {
                        //Logger.LogInfo($"TranslateName: Translated card name (cached): {originalName} -> {translatedName}");
                        callback(new KeyValuePair<string, string>(originalName, translatedName));
                        done = true;
                    }
                }

                if (!done && TranslationHelper.TranslateNameWithSuffix.Value)
                {
                    activeSuffix = Suffixes[gender];
                    suffixedName = string.Concat(originalName, activeSuffix.Key);
                    //Logger.LogDebug($"TranslateName: attempting translation (cached): {suffixedName}");
                    if (GeBoAPI.Instance.AutoTranslationHelper.TryTranslate(suffixedName, out var translatedName2))
                    {
                        translatedName2 = CleanTranslationResult(translatedName2, activeSuffix.Value);
                        if (suffixedName != translatedName2)
                        {
                            //Logger.LogInfo($"TranslateName: Translated card name (cached/suffixed): {originalName} -> {translatedName2}");
                            callback(new KeyValuePair<string, string>(originalName, translatedName2));
                            done = true;
                        }
                    }
                }
            }

            if (!done)
            {
                if (cardTranslationMode == CardLoadTranslationMode.FullyEnabled)
                {
                    // suffixedName will be origName if TranslateNameWithSuffix is off, so just use it here
                    //Logger.LogDebug($"TranslateName: attempting translation (async): {suffixedName}");
                    GeBoAPI.Instance.AutoTranslationHelper.TranslateAsync(suffixedName, (result) =>
                    {
                        if (result.Succeeded)
                        {
                            var translatedName = CleanTranslationResult(result.TranslatedText, activeSuffix.Value);
                            if (suffixedName != translatedName)
                            {
                                //Logger.LogInfo($"TranslateName: Translated card name (async): {originalName} -> {translatedName}");
                                callback(new KeyValuePair<string, string>(originalName, translatedName));
                                return;
                            }
                        }
                        callback(new KeyValuePair<string, string>(originalName, originalName));
                    });
                    yield return null;
                }
                else
                {
                    callback(new KeyValuePair<string, string>(originalName, originalName));
                }
            }
        }

        private string CleanTranslationResult(string input, string suffix)
        {
            return string.Join(" ", input.Split(SpaceSplitter, StringSplitOptions.RemoveEmptyEntries).Select((s) => CleanTranslationResultSection(s, suffix)).ToArray()).Trim();
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
                            output = output.Remove(output.LastIndexOf(testSuffix, StringComparison.InvariantCulture)).TrimEnd();
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
    }
}
