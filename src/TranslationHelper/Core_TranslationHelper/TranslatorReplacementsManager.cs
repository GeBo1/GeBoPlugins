using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.AutoTranslation;
using GeBoCommon.Chara;
using GeBoCommon.Utilities;
using JetBrains.Annotations;
using KKAPI;
using KKAPI.Utilities;
using TranslationHelperPlugin.Chara;
using UnityEngine;
using UnityEngine.SceneManagement;
#if AI || HS2
using AIChara;

#endif

namespace TranslationHelperPlugin
{
    internal class TranslatorReplacementsManager
    {
        internal const int MinLengthJP = 4;
        internal const int MinLength = 4;

        private const float CleanupIdleTime = 2f;

        internal static readonly HashSet<string> SceneNamesThatTriggerReset = new HashSet<string> { "Title" };

        private readonly IEnumerator _cleanupIdleDelay = new WaitForSecondsRealtime(CleanupIdleTime / 2f);

        private readonly HashSet<string> _currentlyRegisteredReplacements;

        private readonly object _lock = new object();
        private readonly HashSet<string> _namesPendingCleanupCheck;
        private readonly Dictionary<string, HashSet<string>> _nameToIDMap;
        private readonly Dictionary<string, WeakReference> _regIDtoCardMap;
        private readonly Dictionary<string, HashSet<string>> _regIDtoNamesMap;
        private Coroutine _currentCleanup;

        private bool _inUse;

        private float _lastBusyTime;

        internal TranslatorReplacementsManager()
        {
            _nameToIDMap = new Dictionary<string, HashSet<string>>();
            _regIDtoNamesMap = new Dictionary<string, HashSet<string>>();
            _currentlyRegisteredReplacements = new HashSet<string>();
            _namesPendingCleanupCheck = new HashSet<string>();
            //trackedRegistrationIDs = new HashSet<string>();
            _regIDtoCardMap = new Dictionary<string, WeakReference>();
            _inUse = false;

            SceneManager.activeSceneChanged += SceneChanged;
        }

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private void UpdateLastBusyTime()
        {
            _lastBusyTime = Time.unscaledTime;
        }

        internal void Deactivate()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            Reset();
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (!_inUse) return;

            UpdateLastBusyTime();
            if (SceneNamesThatTriggerReset.Contains(arg1.name))
            {
                Reset();
                return;
            }

            if (TranslationHelper.RegistrationGameModes.Contains(KoikatuAPI.GetCurrentGameMode()))
            {
                Cleanup();
            }
            else
            {
                Reset();
            }
        }

        private Dictionary<string, string> GetReplacements()
        {
            var _ = this;
            return GeBoAPI.Instance.AutoTranslationHelper.GetReplacements();
        }


        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        private bool CounterAdd(string name, string regId)
        {
            UpdateLastBusyTime();

            lock (_lock)
            {
                if (!_regIDtoNamesMap.TryGetValue(regId, out var regIdNames))
                {
                    _regIDtoNamesMap[regId] = regIdNames = new HashSet<string>();
                }

                if (!_nameToIDMap.TryGetValue(name, out var entries))
                {
                    _nameToIDMap[name] = entries = new HashSet<string>();
                }

                regIdNames.Add(name);
                UpdateLastBusyTime();
                return entries.Add(regId);
            }
        }

        [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Local")]
        private bool CounterRemove(string name, string regId)
        {
            UpdateLastBusyTime();
            lock (_lock)
            {
                if (!_regIDtoNamesMap.TryGetValue(regId, out var regIdNames))
                {
                    _regIDtoNamesMap[regId] = regIdNames = new HashSet<string>();
                }

                if (!_nameToIDMap.TryGetValue(name, out var entries))
                {
                    _nameToIDMap[name] = entries = new HashSet<string>();
                }

                regIdNames.Remove(name);
                UpdateLastBusyTime();
                return entries.Remove(regId);
            }
        }

        private int CounterCount(string name)
        {
            lock (_lock)
            {
                return Math.Max(0, _nameToIDMap.TryGetValue(name, out var value) ? value.Count : 0);
            }
        }

        public bool IsTracked(ChaFile chaFile)
        {
            //return trackedRegistrationIDs.Contains(chaFile.GetRegistrationID());
            lock (_lock)
            {
                return _regIDtoCardMap.ContainsKey(chaFile.GetRegistrationID());
            }
        }

        public bool HaveNamesChanged(ChaFile chaFile)
        {
            //return trackedRegistrationIDs.Contains(chaFile.GetRegistrationID());
            var current = HashSetPool<string>.Get();
            try
            {
                foreach (var name in GetNamesToRegister(chaFile)) current.Add(name.Key);

                lock (_lock)
                {
                    if (!_regIDtoNamesMap.TryGetValue(chaFile.GetRegistrationID(), out var registered))
                    {
                        registered = new HashSet<string>();
                    }

                    return !registered.SetEquals(current);
                }
            }
            finally
            {
                HashSetPool<string>.Release(current);
            }
        }

        protected virtual bool NameQualifiesForRegistration(string name)
        {
            if (name.IsNullOrWhiteSpace()) return false;
            if (CardNameTranslationManager.NameNeedsTranslation(name))
            {
                // short japanese names can pollute cache
                return name.Contains(' ') && name.Length > MinLengthJP;
            }

            return name.Length > MinLength;
        }

        protected virtual IEnumerable<KeyValuePair<string, string>> GetNamesToRegister(ChaFile chaFile)
        {
            var handled = HashSetPool<string>.Get();
            var controller = chaFile.GetTranslationHelperController();
            try
            {
                foreach (var nameEntry in chaFile.EnumerateNames())
                {
                    var name = nameEntry.Value;
                    if (handled.Contains(name)) continue;
                    handled.Add(name);
                    yield return new KeyValuePair<string, string>(name, name);
                    if (controller == null) continue;
                    var origName = controller.OriginalNames[nameEntry.Key];
                    if (origName.IsNullOrEmpty() || origName == name || handled.Contains(origName)) continue;
                    handled.Add(origName);
                    yield return new KeyValuePair<string, string>(origName, origName);
                }

                var fullname = chaFile.GetFullName();
                foreach (var name in new[]
                         {
                             fullname, chaFile.GetOriginalFullName(), chaFile.GetFormattedOriginalName()
                         })
                {
                    if (handled.Contains(name)) continue;
                    handled.Add(name);
                    yield return new KeyValuePair<string, string>(name, name);
                }
            }
            finally
            {
                HashSetPool<string>.Release(handled);
            }
        }

        [PublicAPI]
        public void RegisterReplacementsForNames(params string[] origNames)
        {
            var needsTranslation = ListPool<string>.Get();
            try
            {
                needsTranslation.Capacity = origNames.Length;

                var fastReplacements = DictionaryPool<string, string>.Get();
                try
                {
                    foreach (var origString in origNames)
                    {
                        if (TranslationHelper.TryTranslateName(NameScope.DefaultNameScope, origString,
                                out var translatedString))
                        {
                            //fastReplacements[origString] = origString;
                            fastReplacements[translatedString] = translatedString;
                        }
                        else
                        {
                            needsTranslation.Add(origString);
                        }
                    }

                    if (fastReplacements.Count > 0) RegisterReplacementStrings(fastReplacements);
                }
                finally
                {
                    DictionaryPool<string, string>.Release(fastReplacements);
                }

                foreach (var toTranslate in needsTranslation)
                {
                    void ResultHandler(ITranslationResult result)
                    {
                        var replacements = DictionaryPool<string, string>.Get();
                        try
                        {
                            if (!TranslationHelper.NameNeedsTranslation(toTranslate, NameScope.DefaultNameScope))
                            {
                                replacements[toTranslate] = toTranslate;
                            }

                            if (result.Succeeded)
                            {
                                replacements[result.TranslatedText] = result.TranslatedText;
                            }

                            RegisterReplacementStrings(replacements);
                        }
                        finally
                        {
                            DictionaryPool<string, string>.Release(replacements);
                        }
                    }

                    GeBoAPI.Instance.AutoTranslationHelper.TranslateAsync(toTranslate,
                        NameScope.DefaultNameScope.TranslationScope, ResultHandler);
                }
            }
            finally
            {
                ListPool<string>.Release(needsTranslation);
            }
        }

        public void RegisterReplacementStrings(IEnumerable<KeyValuePair<string, string>> replacementStrings)
        {
            var replacements = GetReplacements();
            if (replacements == null) return;
            foreach (var replacementString in replacementStrings)
            {
                if (replacements.ContainsKey(replacementString.Key)) continue;
                Logger.DebugLogDebug(
                    $"Registering as translation replacement: {replacementString.Key} => {replacementString.Value}");
                replacements.Add(replacementString.Key, replacementString.Value);
            }
        }

        public void Track(ChaFile chaFile)
        {
            UpdateLastBusyTime();

            _inUse = true;
            var regID = chaFile.GetRegistrationID();

            lock (_lock)
            {
                if (_regIDtoCardMap.ContainsKey(regID)) return;

                _regIDtoCardMap[regID] = new WeakReference(chaFile);
                //Logger.LogDebug($"Attempting to register translation replacement: {regID} {chaFile.GetFullName()}");
                var replacements = GetReplacements();
                if (replacements == null) return;

                foreach (var nameEntry in GetNamesToRegister(chaFile))
                {
                    var name = nameEntry.Key;
                    if (name.IsNullOrEmpty() || !NameQualifiesForRegistration(name)) continue;
                    if (!replacements.ContainsKey(name))
                    {
                        Logger.DebugLogDebug($"Registering as translation replacement: {name} => {nameEntry.Value}");
                        replacements.Add(name, nameEntry.Value);
                        _currentlyRegisteredReplacements.Add(name);
                    }

                    CounterAdd(name, regID);
                }
            }
        }

        public void Untrack(ChaFile chaFile)
        {
            UpdateLastBusyTime();

            var regID = chaFile.GetRegistrationID();
            //Logger.LogDebug($"Attempting to unregister translation replacements: {regID} {chaFile.GetFullName()}");
            var toClean = HashSetPool<string>.Get();
            try
            {
                lock (_lock)
                {
                    // make a copy of current entries in _regIDtoNamesMap
                    var namesToCheck = ListPool<string>.Get();
                    try
                    {
                        if (_regIDtoNamesMap.TryGetValue(regID, out var tmp)) namesToCheck.AddRange(tmp);
                        foreach (var name in namesToCheck)
                        {
                            if (string.IsNullOrEmpty(name)) continue;
                            CounterRemove(name, regID);

                            if (CounterCount(name) != 0 || !_currentlyRegisteredReplacements.Contains(name)) continue;
                            toClean.Add(name);
                        }
                    }
                    finally
                    {
                        ListPool<string>.Release(namesToCheck);
                    }

                    _regIDtoCardMap.Remove(regID);
                }

                foreach (var name in toClean)
                {
                    CheckNamesForCleanup(name);
                }
            }
            finally
            {
                HashSetPool<string>.Release(toClean);
            }
        }

        private void CheckNamesForCleanup(params string[] names)
        {
            lock (_lock)
            {
                foreach (var name in names)
                {
                    _namesPendingCleanupCheck.Add(name);
                }
            }

            RequestCleanupCheck();
        }

        private void RequestCleanupCheck()
        {
            UpdateLastBusyTime();


            lock (_lock)
            {
                if (_currentCleanup != null) return;

                _currentCleanup = TranslationHelper.Instance.StartCoroutine(
                    DoCleanupCheck().AppendCo(() =>
                    {
                        lock (_lock)
                        {
                            _currentCleanup = null;
                        }
                    }));
            }
        }

        private IEnumerator DoCleanupCheck()
        {
            var workNames = HashSetPool<string>.Get();
            try
            {
                int UpdateWorkNames()
                {
                    if (workNames.Count > 0) return workNames.Count;
                    lock (_lock)
                    {
                        if (_namesPendingCleanupCheck.Count == 0) return 0;
                        foreach (var name in _namesPendingCleanupCheck.ToList())
                        {
                            _namesPendingCleanupCheck.Remove(name);
                            workNames.Add(name);
                        }
                    }

                    return workNames.Count;
                }

                var replacements = GetReplacements();
                while (UpdateWorkNames() > 0)
                {
                    var idleTimeEnd = _lastBusyTime + CleanupIdleTime;
                    while (Time.unscaledTime <= idleTimeEnd)
                    {
                        yield return _cleanupIdleDelay;
                    }

                    var name = workNames.FirstOrDefault();
                    workNames.Remove(name);
                    if (string.IsNullOrEmpty(name)) continue;

                    yield return null;

                    lock (_lock)
                    {
                        if (!_currentlyRegisteredReplacements.Contains(name) || CounterCount(name) != 0) continue;
                        if (replacements?.Remove(name) ?? false)
                        {
                            Logger.DebugLogDebug($"Unregistering as translation replacement: {name}");
                        }

                        _currentlyRegisteredReplacements.Remove(name);
                    }
                }
            }
            finally
            {
                HashSetPool<string>.Release(workNames);
            }
        }

        public void Cleanup(bool deepClean = false)
        {
            if (!_inUse) return;

            var orig = _nameToIDMap.Count;
            int current;

            var namesToRemove = HashSetPool<string>.Get();
            try
            {
                lock (_lock)
                {
                    if (deepClean)
                    {
                        var deadEntries = _regIDtoCardMap
                            .Where(e => !e.Value.IsAlive || (e.Value.Target as ChaFile).GetRegistrationID() != e.Key)
                            .Select(e => e.Key).ToList();

                        foreach (var deadEntry in deadEntries)
                        {
                            Logger.DebugLogDebug($"Cleanup: dead entry: {deadEntry}");
                            _regIDtoCardMap.Remove(deadEntry);
                        }

                        foreach (var entry in _nameToIDMap)
                        {
                            var removed = entry.Value.RemoveWhere(e => deadEntries.Contains(e));
                            if (removed > 0)
                            {
                                Logger.DebugLogDebug($"Cleanup: removed {removed} entries for {entry.Key}");
                            }
                        }
                    }

                    foreach (var name in _nameToIDMap.Keys.AsEnumerable().Where(key => _nameToIDMap[key].Count == 0)
                                 .ToList())
                    {
                        namesToRemove.Add(name);
                        _nameToIDMap.Remove(name);
                    }

                    current = _nameToIDMap.Count;
                    _inUse = current > 0;
                }

                CheckNamesForCleanup(namesToRemove.ToArray());
            }
            finally
            {
                HashSetPool<string>.Release(namesToRemove);
            }

            if (orig <= current) return;
            Logger.DebugLogDebug(
                $"{nameof(TranslatorReplacementsManager)}.{nameof(Cleanup)}: removed {orig - current} entries ({current} remain)");
        }

        public void Reset()
        {
            var replacements = GetReplacements();

            lock (_lock)
            {
                if (_currentCleanup != null) TranslationHelper.Instance.StopCoroutine(_currentCleanup);
                _currentCleanup = null;
                _cleanupIdleDelay.Reset();
                foreach (var name in _currentlyRegisteredReplacements)
                {
                    replacements?.Remove(name);
                }

                _namesPendingCleanupCheck.Clear();
                _regIDtoCardMap.Clear();
                _regIDtoNamesMap.Clear();
                _currentlyRegisteredReplacements.Clear();
                _nameToIDMap.Clear();
                _inUse = false;
            }
        }
    }
}
