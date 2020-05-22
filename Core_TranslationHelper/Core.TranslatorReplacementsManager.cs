using BepInEx.Logging;
using GeBoCommon;
using GeBoCommon.Chara;
using System.Collections.Generic;
using System.Linq;

#if AI
using AIChara;
#endif

namespace TranslationHelperPlugin
{
    internal class TranslatorReplacementsManager
    {
        internal static ManualLogSource Logger => TranslationHelper.Logger;

        private readonly Dictionary<string, HashSet<string>> regCounter;
        private readonly HashSet<string> currentRegistrations;
        private readonly HashSet<string> trackedRegistrationIDs;

        internal TranslatorReplacementsManager()
        {
            regCounter = new Dictionary<string, HashSet<string>>();
            currentRegistrations = new HashSet<string>();
            trackedRegistrationIDs = new HashSet<string>();
        }

        private Dictionary<string, string> GetReplacements() => GeBoAPI.Instance.AutoTranslationHelper.GetReplacements();

        private bool CounterAdd(string name, string regId)
        {
            if (!regCounter.TryGetValue(name, out var entries))
            {
                regCounter[name] = entries = new HashSet<string>();
            }
            return entries.Add(regId);
        }

        private bool CounterRemove(string name, string regId)
        {
            if (regCounter.TryGetValue(name, out var value))
            {
                return value.Remove(regId);
            }
            return false;
        }

        private int CounterCount(string name)
        {
            if (regCounter.TryGetValue(name, out var value))
            {
                return value.Count;
            }
            return 0;
        }

        public bool IsTracked(ChaFile chaFile)
        {
            return trackedRegistrationIDs.Contains(chaFile.GetRegistrationID());
        }

        public void Track(ChaFile chaFile)
        {
            var regID = chaFile.GetRegistrationID();
            if (trackedRegistrationIDs.Contains(regID))
            {
                return;
            }
            trackedRegistrationIDs.Add(regID);
            //Logger.LogDebug($"Attempting to register translation replacement: {regID}");
            var replacements = GetReplacements();
            if (replacements == null)
            {
                return;
            }

            var handled = new HashSet<string>();

            var names = chaFile.EnumerateNames().Select((n) => n.Value).ToList();
            names.Add(chaFile.GetFullName());

            //Logger.LogFatal($"fullname={chaFile.GetFullName()}");
            foreach (var name in names.Where((n) => n.Length > 2))
            {
                if (handled.Contains(name))
                {
                    continue;
                }
                handled.Add(name);

                if (!name.IsNullOrEmpty())
                {
                    if (!replacements.ContainsKey(name))
                    {
                        //Logger.LogInfo($"Registering translation replacement: {name}");
                        replacements.Add(name, name);
                        currentRegistrations.Add(name);
                    }
                    CounterAdd(name, regID);
                }
            }
        }

        public void Untrack(ChaFile chaFile)
        {
            var regID = chaFile.GetRegistrationID();
            //Logger.LogDebug($"Attempting to unregister translation replacements: {regID}");
            var replacements = GetReplacements();
            foreach (var namePair in chaFile.EnumerateNames())
            {
                var name = namePair.Value;
                //Logger.LogDebug($"Attempting to unregister translation replacements: {regID} / {name}");
                if (!name.IsNullOrEmpty())
                {
                    CounterRemove(name, chaFile.GetRegistrationID());

                    if (CounterCount(name) == 0 && currentRegistrations.Contains(name))
                    {
                        replacements?.Remove(name);
                        currentRegistrations.Remove(name);
                    }
                }
            }
            if (trackedRegistrationIDs.Contains(regID))
            {
                trackedRegistrationIDs.Remove(regID);
            }
        }

        public void Cleanup()
        {
            foreach (var key in regCounter.Keys.ToArray().Where(key => regCounter[key].Count == 0))
            {
                regCounter.Remove(key);
            }
        }

        public void Reset()
        {
            var replacements = GetReplacements();
            foreach (var name in currentRegistrations)
            {
                replacements?.Remove(name);
            }
            trackedRegistrationIDs.Clear();
            currentRegistrations.Clear();
            regCounter.Clear();
        }
    }
}
