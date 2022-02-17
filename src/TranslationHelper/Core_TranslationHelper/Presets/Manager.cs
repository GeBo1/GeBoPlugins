using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI;
using TranslationHelperPlugin.Chara;
using TranslationHelperPlugin.Presets.Data;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
#if AI||HS2
using AIChara;

#endif

namespace TranslationHelperPlugin.Presets
{
    public class Manager
    {
        private static bool _nicknamesSupported;

        private static readonly Dictionary<string, string> AliasAutoSuffixes = new Dictionary<string, string>
        {
            // ReSharper disable StringLiteralTypo
            { "先生", " Sensei" },
            { "せんせい", " Sensei" },
            { "さん", "-san" },
            { "はん", "-han" },
            { "君", "-kun" },
            { "くん", "-kun" },
            { "ちゃん", "-chan" },
            { "先輩", " Senpai" },
            { "せんぱい", " Senpai" },
            { "後輩", " Kōhai" },
            { "こうはい", " Kōhai" },
            { "様", " Sama" },
            { "さま", " Sama" }
            // ReSharper restore StringLiteralTypo
        };

        private static readonly string[] NoNameNameEntries = { string.Empty };

        private readonly Dictionary<CharacterSex, Dictionary<CardNameCacheKey, CardNameCacheValue>> _cardCache;
        private readonly Dictionary<CharacterSex, Dictionary<string, string>> _fullNameCache;

        private readonly Dictionary<CharacterSex, Dictionary<CardNameCacheKey, Dictionary<string, string>>>
            _nickNameCache;

        private readonly HitMissCounter _stats;

        private bool _hasEntries;
        private int _presetCount;
        private bool _reverseNames;

        public Manager()
        {
            var comparer = EnumEqualityComparer<CharacterSex>.Comparer;
            _stats = new HitMissCounter(this.GetPrettyTypeFullName());
            _cardCache = new Dictionary<CharacterSex, Dictionary<CardNameCacheKey, CardNameCacheValue>>(comparer);
            _fullNameCache = new Dictionary<CharacterSex, Dictionary<string, string>>(comparer);
            _nickNameCache =
                new Dictionary<CharacterSex, Dictionary<CardNameCacheKey, Dictionary<string, string>>>(comparer);
            Reset();
            TranslationHelper.CardTranslationBehaviorChanged += CardTranslationHelperBehaviorChanged;
            KoikatuAPI.Quitting += ApplicationQuitting;
        }

        private static ManualLogSource Logger => TranslationHelper.Logger;

        private void ApplicationQuitting(object sender, EventArgs e)
        {
            LogCacheStats(nameof(ApplicationQuitting));
        }

        private void CardTranslationHelperBehaviorChanged(object sender, EventArgs e)
        {
            Reset();
        }

        private static void ResetCache<T>(IDictionary<CharacterSex, T> cache, Func<T> initializer = null)
            where T : new()
        {
            foreach (var entry in Enum.GetValues(typeof(CharacterSex)))
            {
                var key = (CharacterSex)entry;
                if (cache.TryGetValue(key, out var val))
                {
                    switch (val)
                    {
                        case IDictionary dictVal:
                            dictVal.Clear();
                            continue;
                        case IList listVal:
                            listVal.Clear();
                            continue;
                    }
                }

                cache[(CharacterSex)entry] = initializer != null ? initializer() : new T();
            }
        }

        internal void Reset()
        {
            ResetCache(_cardCache);
            ResetCache(_fullNameCache, TranslationHelper.StringCacheInitializer);
            ResetCache(_nickNameCache);
            _presetCount = 0;

            if (TranslationHelper.IsShuttingDown) return;

            _nicknamesSupported = GeBoAPI.Instance != null && GeBoAPI.Instance.ChaFileNameToIndex("nickname") >= 0;

            _hasEntries = false;

            _reverseNames = TranslationHelper.ShowGivenNameFirst;


            // don't load presets if we're not going to use them
            if (TranslationHelper.Instance == null ||
                !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled)
            {
                return;
            }

            var nameCacheDone = new Dictionary<CharacterSex, HashSet<string>>(
                EnumEqualityComparer<CharacterSex>.Comparer);
            var aliases = TranslationHelper.StringCacheInitializer();
            var start = Time.realtimeSinceStartup;
            foreach (var preset in NamePresets.Load())
            {
                _presetCount++;
                var translation = preset.GetTranslation(AutoTranslatorSettings.DestinationLanguage);
                if (translation == null) continue;

                var translatedName = BuildFullName(translation.FamilyName, translation.GivenName);

                ResetCache(nameCacheDone);
                var familyNames = preset.FamilyNames.Count > 0 ? preset.FamilyNames : NoNameNameEntries.ToList();
                var givenNames = preset.GivenNames.Count > 0 ? preset.GivenNames : NoNameNameEntries.ToList();

                foreach (var family in familyNames)
                {
                    foreach (var given in givenNames)
                    {
                        if (string.IsNullOrEmpty(family) && string.IsNullOrEmpty(given)) continue;
                        var origName = BuildFullName(family, given);
                        if (nameCacheDone[preset.Sex].Contains(origName)) continue;

                        aliases.Clear();

                        nameCacheDone[preset.Sex].Add(origName);
                        _fullNameCache[preset.Sex][origName] = translatedName;
                        TranslationHelper.Instance.AddTranslatedNameToCache(origName, translatedName);

                        var key = new CardNameCacheKey(family, given);
                        _cardCache[preset.Sex][key] = new CardNameCacheValue(
                            translation.FamilyName,
                            translation.GivenName);

                        if (!_nicknamesSupported) continue;
                        if (!_nickNameCache[preset.Sex].TryGetValue(key, out var nickCache))
                        {
                            _nickNameCache[preset.Sex][key] = nickCache = TranslationHelper.StringCacheInitializer();
                        }


                        // add alternate given name spellings as possible nicknames as well as being suffixed
                        foreach (var alias in preset.GivenNames)
                        {
                            aliases[alias] = translation.GivenName;
                        }

                        // add family name variants so they can be suffixed
                        foreach (var alias in preset.FamilyNames)
                        {
                            aliases[alias] = translation.FamilyName;
                        }

                        // remove current given name
                        aliases.Remove(given);
                        foreach (var rawFamilyName in preset.FamilyNames)
                        {
                            aliases.Remove(rawFamilyName);
                        }


                        foreach (var nick in preset.NickNames)
                        {
                            nickCache[nick] = translation.NickName;
                        }

                        AddSuffixedNicknames(nickCache);

                        foreach (var alias in aliases.Where(alias => !nickCache.ContainsKey(alias.Key)))
                        {
                            nickCache[alias.Key] = alias.Value;
                        }
                    }
                }

                _hasEntries |= nameCacheDone.Any(k => k.Value.Count > 0);
            }

            Logger.LogDebug($"Loaded {_presetCount} preset(s) in {Time.realtimeSinceStartup - start} second(s)");
        }

        private static void AddSuffixedNicknames(IDictionary<string, string> nickCache)
        {
            // only bother with this if game supports nicknames
            if (!_nicknamesSupported) return;

            var suffixedNicknames = TranslationHelper.StringCacheInitializer();

            foreach (var nick in nickCache.Where(n => !NickNameIsSuffixed(n.Key)))
            {
                foreach (var suffix in AliasAutoSuffixes)
                {
                    suffixedNicknames[string.Concat(nick.Key, suffix.Key)] = string.Concat(nick.Value, suffix.Value);
                }
            }

            foreach (var newNick in suffixedNicknames)
            {
                if (!nickCache.ContainsKey(newNick.Key))
                {
                    nickCache[newNick.Key] = newNick.Value;
                }
            }
        }

        private static bool NickNameIsSuffixed(string nick)
        {
            return AliasAutoSuffixes.Keys.Any(nick.EndsWith);
        }

        private string BuildFullName(string family, string given)
        {
            return _reverseNames
                ? StringUtils.JoinStrings(" ", given, family).Trim()
                : StringUtils.JoinStrings(" ", family, given).Trim();
        }

        private string BuildFullName(ICardNameCacheBase cacheBase)
        {
            return cacheBase.FullName ?? BuildFullName(cacheBase.FamilyName, cacheBase.GivenName);
        }

        [UsedImplicitly]
        protected IEnumerable<string> BuildFullNames(IEnumerable<string> familyNames, IEnumerable<string> givenNames)
        {
            return (from family in familyNames
                from given in givenNames
                select BuildFullName(family, given)).Distinct();

            /*
            var done = new HashSet<string>();
            var tmpGivenNames = givenNames.ToList();

            foreach (var family in familyNames)
            {
                foreach (var result in tmpGivenNames.Select(given => BuildFullName(family, given)).Where(result => !done.Contains(result)))
                {
                    yield return result;
                    done.Add(result);
                }
            }
            */
        }

        public bool TryTranslateFullName(CharacterSex sex, string origName, out string result)
        {
            if (_hasEntries && _fullNameCache[sex].TryGetValue(origName, out result))
            {
                if (!TranslationHelper.NameStringComparer.Equals(origName, result) &&
                    !CardNameTranslationManager.NameNeedsTranslation(result))
                {
                    CardNameTranslationManager.CacheRecentTranslation(new NameScope(sex), origName, result);
                }

                _stats.RecordHit();
                return true;
            }

            _stats.RecordMiss();
            result = null;
            return false;
        }

        public void ApplyPresetResults(ChaFile chaFile, Dictionary<string, string> result,
            Action<int, string> callback = null)
        {
            foreach (var entry in result)
            {
                var i = GeBoAPI.Instance.ChaFileNameToIndex(entry.Key);
                if (i < 0 || string.IsNullOrEmpty(entry.Value) || chaFile.GetName(i) == entry.Value)
                {
                    continue;
                }

                chaFile.SetTranslatedName(i, entry.Value);
                callback?.Invoke(i, entry.Value);
            }
        }

        private void CachePresetResults(ChaFile chaFile, Dictionary<string, string> result)
        {
            var sex = chaFile.GetSex();
            foreach (var entry in result)
            {
                Logger.DebugLogDebug(
                    $"{this.GetPrettyTypeFullName()}.{nameof(CachePresetResults)}: ({entry.Key}, {entry.Value})");
                if (entry.Value.IsNullOrEmpty()) continue;
                var i = GeBoAPI.Instance.ChaFileNameToIndex(entry.Key);
                var origName = chaFile.GetName(i);
                Logger.DebugLogDebug(
                    $"{this.GetPrettyTypeFullName()}.{nameof(CachePresetResults)}: ({entry.Key}, {entry.Value}): origName={origName}");
                if (TranslationHelper.NameStringComparer.Equals(origName, entry.Value) ||
                    CardNameTranslationManager.NameNeedsTranslation(entry.Value))
                {
                    continue;
                }

                var nameType = chaFile.GetNameType(i);
                var scope = new NameScope(sex, nameType);

                Action<string> callback = null;
                if (entry.Key == "fullname")
                {
                    var handled = HashSetPool<string>.Get();
                    try
                    {
                        handled.Add(origName);
                        foreach (var targetName in IterateTargetNames(chaFile))
                        {
                            if (handled.Contains(targetName)) continue;
                            handled.Add(targetName);
                            Logger.DebugLogDebug(
                                $"{this.GetPrettyTypeFullName()}.{nameof(CachePresetResults)}: ({entry.Key}, {entry.Value}): targetName={targetName}");
                            callback = CardNameTranslationManager.MakeCachingCallbackWrapper(targetName, chaFile, scope,
                                callback);
                        }
                    }
                    finally
                    {
                        HashSetPool<string>.Release(handled);
                    }
                }

                callback = CardNameTranslationManager.MakeCachingCallbackWrapper(origName, chaFile, scope, callback);
                callback = CharaFileInfoTranslationManager.MakeCachingCallbackWrapper(origName, chaFile, scope,
                    callback);
                callback(entry.Value);
            }

            IEnumerable<string> IterateTargetNames(ChaFile cha)
            {
                yield return chaFile.GetOriginalFullName();
                yield return chaFile.GetFormattedOriginalName();
            }
        }

        public bool TryTranslateCardNames(ChaFile chaFile, out Dictionary<string, string> result)
        {
            Logger.DebugLogDebug(
                $"{this.GetPrettyTypeFullName()}.{nameof(TryTranslateCardNames)}: {chaFile} {chaFile.GetFullName()}");
            if (!_hasEntries)
            {
                _stats.RecordMiss();
                Logger.DebugLogDebug(
                    $"{this.GetPrettyTypeFullName()}.{nameof(TryTranslateCardNames)}: {chaFile} {chaFile.GetFullName()}: FALSE: no entries");
                result = null;
                return false;
            }

            void PopulateResult(IDictionary<string, string> output, string nameType, string nameValue)
            {
                Logger.DebugLogDebug($"{nameof(PopulateResult)}: {nameType} {nameValue}");
                if (nameValue != null) output[nameType] = nameValue;
            }

            result = null;
            var key = new CardNameCacheKey(chaFile);
            var sex = chaFile.GetSex();
            if (!_cardCache[sex].TryGetValue(key, out var match))
            {
                _stats.RecordMiss();
                Logger.DebugLogDebug(
                    $"{this.GetPrettyTypeFullName()}.{nameof(TryTranslateCardNames)}: {chaFile} {chaFile.GetFullName()}: FALSE: no match");
                return false;
            }

            Logger.DebugLogDebug(
                $"{this.GetPrettyTypeFullName()}.{nameof(TryTranslateCardNames)}: {chaFile} {chaFile.GetFullName()}: {match}");

            result = new Dictionary<string, string>(GeBoAPI.Instance.ChaFileNameCount + 1);
            var fullName = BuildFullName(match);
            PopulateResult(result, "fullname", fullName);
            PopulateResult(result, "firstname", match.GivenName);
            PopulateResult(result, "lastname", match.FamilyName);


            var origNick = chaFile.GetName("nickname");
            if (!string.IsNullOrEmpty(origNick))
            {
                if (_nickNameCache[sex].TryGetValue(key, out var nickLookup) &&
                    nickLookup.TryGetValue(origNick, out var translatedNick))
                {
                    PopulateResult(result, "nickname", translatedNick);
                }
                else if (key.GivenName == origNick)
                {
                    PopulateResult(result, "nickname", match.GivenName);
                }
            }

            CachePresetResults(chaFile, result);
            _stats.RecordHit();
            return true;
        }

        protected void LogCacheStats(string prefix)
        {
            Logger?.LogDebug(_stats.GetCounts(prefix, _presetCount));
        }
    }
}
