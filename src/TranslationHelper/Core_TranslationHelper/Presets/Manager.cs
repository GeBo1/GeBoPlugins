using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
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
            {"先生", " Sensei"},
            {"せんせい", " Sensei"},
            {"さん", "-san"},
            {"はん", "-han"},
            {"君", "-kun"},
            {"くん", "-kun"},
            {"ちゃん", "-chan"},
            {"先輩", " Senpai"},
            {"せんぱい", " Senpai"},
            {"後輩", " Kōhai"},
            {"こうはい", " Kōhai"},
            {"様", " Sama"},
            {"さま", " Sama"}
            // ReSharper restore StringLiteralTypo
        };

        private readonly Dictionary<CharacterSex, Dictionary<CardNameCacheKey, CardNameCacheValue>> _cardCache;
        private readonly Dictionary<CharacterSex, Dictionary<string, string>> _fullNameCache;

        private readonly Dictionary<CharacterSex, Dictionary<CardNameCacheKey, Dictionary<string, string>>>
            _nickNameCache;

        private bool _hasEntries;
        private bool _reverseNames;

        public Manager()
        {
            var comparer = EnumEqualityComparer<CharacterSex>.Comparer;
            _cardCache = new Dictionary<CharacterSex, Dictionary<CardNameCacheKey, CardNameCacheValue>>(comparer);
            _fullNameCache = new Dictionary<CharacterSex, Dictionary<string, string>>(comparer);
            _nickNameCache = new Dictionary<CharacterSex, Dictionary<CardNameCacheKey, Dictionary<string, string>>>(comparer);
            Reset();
            TranslationHelper.BehaviorChanged += TranslationHelperBehaviorChanged;
        }

        private static ManualLogSource Logger => TranslationHelper.Logger;

        private void TranslationHelperBehaviorChanged(object sender, EventArgs e)
        {
            Reset();
        }

        private void ResetCache<T>(Dictionary<CharacterSex, T> cache, Func<T> initializer = null) where T : new()
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

            if (TranslationHelper.IsShuttingDown) return;

            _nicknamesSupported = GeBoAPI.Instance != null && GeBoAPI.Instance.ChaFileNameToIndex("nickname") >= 0;

            var presetCount = 0;
            _hasEntries = false;

            _reverseNames = TranslationHelper.ShowGivenNameFirst;


            // don't load presets if we're not going to use them
            if (TranslationHelper.Instance == null || !TranslationHelper.Instance.CurrentCardLoadTranslationEnabled) return;

            var nameCacheDone = new Dictionary<CharacterSex, HashSet<string>>(
                EnumEqualityComparer<CharacterSex>.Comparer);
            var aliases = TranslationHelper.StringCacheInitializer();
            var start = Time.realtimeSinceStartup;
            foreach (var preset in NamePresets.Load())
            {
                presetCount++;
                var translation = preset.GetTranslation(AutoTranslatorSettings.DestinationLanguage);
                if (translation == null) continue;

                var translatedName = BuildFullName(translation.FamilyName, translation.GivenName);

                ResetCache(nameCacheDone);
                var familyNames = preset.FamilyNames.Count > 0
                    ? preset.FamilyNames
                    : new List<string>(new[] {string.Empty});
                var givenNames = preset.GivenNames.Count > 0
                    ? preset.GivenNames
                    : new List<string>(new[] {string.Empty});

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

            Logger.LogDebug($"Loaded {presetCount} preset(s) in {Time.realtimeSinceStartup - start} second(s)");
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
                    !StringUtils.ContainsJapaneseChar(result))
                {
                    CardNameTranslationManager.CacheRecentTranslation(new NameScope(sex), origName, result);
                }

                return true;
            }
            result = null;
            return false;
        }

        public bool TryTranslateCardNames(ChaFile chaFile, out Dictionary<string, string> result)
        {
            var origName = chaFile.GetFullName();
            if (!_hasEntries)
            {
                result = null;
                return false;
            }

            void PopulateResult(IDictionary<string, string> output, string nameType, string nameValue)
            {
                if (nameValue != null) output[nameType] = nameValue;
            }

            result = null;
            var key = new CardNameCacheKey(chaFile);
            var sex = chaFile.GetSex();
            if (!_cardCache[sex].TryGetValue(key, out var match))
            {
                return false;
            }


            result = new Dictionary<string, string>(GeBoAPI.Instance.ChaFileNameCount);
            PopulateResult(result, "fullname", match.FullName);
            PopulateResult(result, "firstname", match.GivenName);
            PopulateResult(result, "lastname", match.FamilyName);

            var fullName = BuildFullName(match);
            if (!StringUtils.ContainsJapaneseChar(fullName))
            {
                CardNameTranslationManager.CacheRecentTranslation(new NameScope(sex), origName, fullName);
                var fullPath = chaFile.GetFullPath();
                if (!string.IsNullOrEmpty(fullPath)) CharaFileInfoTranslationManager.CacheRecentTranslation(
                    new NameScope(chaFile.GetSex()), fullPath, fullName);
            }


            var origNick = chaFile.GetName("nickname");
            if (string.IsNullOrEmpty(origNick)) return true;

            if (_nickNameCache[sex].TryGetValue(key, out var nickLookup) &&
                nickLookup.TryGetValue(origNick, out var translatedNick))
            {
                PopulateResult(result, "nickname", translatedNick);
            }
            else if (key.GivenName == origNick)
            {
                PopulateResult(result, "nickname", match.GivenName);
            }

            return true;
        }
    }
}
