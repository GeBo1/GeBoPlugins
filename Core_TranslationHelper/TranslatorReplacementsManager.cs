using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using KKAPI;
using TranslationHelperPlugin.Chara;
using UnityEngine.SceneManagement;

#if AI || HS2
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    internal class TranslatorReplacementsManager
    {
        internal static readonly HashSet<string> SceneNamesThatTriggerReset = new HashSet<string> {"Title"};

        private readonly HashSet<string> _currentlyRegisteredReplacements;
        private readonly Dictionary<string, HashSet<string>> _nameToIDMap;
        private readonly Dictionary<string, WeakReference> _regIDtoCardMap;

        private bool _inUse;

        internal TranslatorReplacementsManager()
        {
            _nameToIDMap = new Dictionary<string, HashSet<string>>();
            _currentlyRegisteredReplacements = new HashSet<string>();
            //trackedRegistrationIDs = new HashSet<string>();
            _regIDtoCardMap = new Dictionary<string, WeakReference>();
            _inUse = false;

            SceneManager.activeSceneChanged += SceneChanged;
        }

        internal static ManualLogSource Logger => TranslationHelper.Logger;

        internal void Deactivate()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            Reset();
        }

        private void SceneChanged(Scene arg0, Scene arg1)
        {
            if (!_inUse) return;

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
            return GeBoAPI.Instance.AutoTranslationHelper.GetReplacements();
        }

        private bool CounterAdd(string name, string regId)
        {
            if (_nameToIDMap.TryGetValue(name, out var entries)) return entries.Add(regId);
            _nameToIDMap[name] = entries = new HashSet<string>();
            return entries.Add(regId);
        }

        private bool CounterRemove(string name, string regId)
        {
            return _nameToIDMap.TryGetValue(name, out var value) && value.Remove(regId);
        }

        private int CounterCount(string name)
        {
            return Math.Max(0, _nameToIDMap.TryGetValue(name, out var value) ? value.Count : 0);
        }

        public bool IsTracked(ChaFile chaFile)
        {
            //return trackedRegistrationIDs.Contains(chaFile.GetRegistrationID());
            return _regIDtoCardMap.ContainsKey(chaFile.GetRegistrationID());
        }

        private IEnumerable<string> GetNamesToRegister(ChaFile chaFile)
        {
            var handled = new HashSet<string>();
            foreach (var name in chaFile.EnumerateNames().Select(n => n.Value))
            {
                if (handled.Contains(name)) continue;
                handled.Add(name);
                yield return name;
            }

            var fullname = chaFile.GetFullName();
            if (handled.Contains(fullname)) yield break;
            yield return fullname;
        }

        public void Track(ChaFile chaFile)
        {
            _inUse = true;
            var regID = chaFile.GetRegistrationID();
            if (_regIDtoCardMap.ContainsKey(regID)) return;

            _regIDtoCardMap[regID] = new WeakReference(chaFile);
            //Logger.LogDebug($"Attempting to register translation replacement: {regID} {chaFile.GetFullName()}");
            var replacements = GetReplacements();
            if (replacements == null) return;

            //Logger.LogFatal($"fullname={chaFile.GetFullName()}");
            foreach (var name in GetNamesToRegister(chaFile).Where(n => n.Length > 2))
            {
                if (name.IsNullOrEmpty()) continue;
                if (!replacements.ContainsKey(name))
                {
                    Logger.LogDebug($"Registering translation replacement: {name}");
                    replacements.Add(name, name);
                    _currentlyRegisteredReplacements.Add(name);
                }

                CounterAdd(name, regID);
            }
        }

        public void Untrack(ChaFile chaFile)
        {
            var regID = chaFile.GetRegistrationID();
            //Logger.LogDebug($"Attempting to unregister translation replacements: {regID} {chaFile.GetFullName()}");
            var replacements = GetReplacements();
            foreach (var name in GetNamesToRegister(chaFile))
            {
                //Logger.LogDebug($"Attempting to unregister translation replacements: {regID} / {name}");
                if (name.IsNullOrEmpty()) continue;
                CounterRemove(name, chaFile.GetRegistrationID());

                if (CounterCount(name) != 0 || !_currentlyRegisteredReplacements.Contains(name)) continue;

                replacements?.Remove(name);
                _currentlyRegisteredReplacements.Remove(name);
            }

            _regIDtoCardMap.Remove(regID);
        }

        public void Cleanup(bool deepClean = false)
        {
            if (!_inUse) return;

            var orig = _nameToIDMap.Count;


            if (deepClean)
            {
                var deadEntries = _regIDtoCardMap
                    .Where(e => !e.Value.IsAlive || (e.Value.Target as ChaFile).GetRegistrationID() != e.Key)
                    .Select(e => e.Key).ToList();

                foreach (var deadEntry in deadEntries)
                {
                    Logger.LogDebug($"Cleanup: dead entry: {deadEntry}");
                    _regIDtoCardMap.Remove(deadEntry);
                }

                foreach (var entry in _nameToIDMap)
                {
                    var removed = entry.Value.RemoveWhere(e => deadEntries.Contains(e));
                    if (removed > 0)
                    {
                        Logger.LogDebug($"Cleanup: removed {removed} entries for {entry.Key}");
                    }
                }
            }


            var replacements = GetReplacements();
            var namesToRemove = _nameToIDMap.Keys.AsEnumerable().Where(key => _nameToIDMap[key].Count == 0).ToList();
            foreach (var key in namesToRemove)
            {
                _nameToIDMap.Remove(key);
                if (!_currentlyRegisteredReplacements.Contains(key)) continue;
                replacements?.Remove(key);
                _currentlyRegisteredReplacements.Remove(key);
            }

            var current = _nameToIDMap.Count;
            _inUse = current > 0;

            Logger.LogDebug($"Cleanup: removed {orig - current} entries ({current} remain)");
        }

        public void Reset()
        {
            var replacements = GetReplacements();
            foreach (var name in _currentlyRegisteredReplacements)
            {
                replacements?.Remove(name);
            }

            _regIDtoCardMap.Clear();
            _currentlyRegisteredReplacements.Clear();
            _nameToIDMap.Clear();
            _inUse = false;
        }
    }
}
