using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GeBoCommon;
using KKAPI.Utilities;
using UnityEngine;
using KeyboardShortcut = BepInEx.Configuration.KeyboardShortcut;

namespace TranslationCacheCleanerPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TranslationCacheCleaner
    {
        public const string GUID = "com.gebo.bepinex.translationcachecleaner";
        public const string PluginName = "Translation Cache Cleaner";
        public const string Version = "0.5.2";

        private const float NotifySeconds = 10f;
        private const float YieldSeconds = 0.1f;

        internal static new ManualLogSource Logger;

        public static ConfigEntry<KeyboardShortcut> CleanCacheHotkey { get; private set; }

        private static bool cleaningActive;
        private string latestBackup;

        internal void Awake()
        {
            Logger = base.Logger;

            CleanCacheHotkey = Config.Bind("Keyboard Shortcuts", "Clean Cache Hotkey", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftShift), "Pressing this will attempt to clean your autotranslation cache.");
        }

        internal void Update()
        {
            if (!cleaningActive && CleanCacheHotkey.Value.IsPressed())
            {
                try
                {
                    cleaningActive = true;
                    StartCoroutine(CoroutineUtils.ComposeCoroutine(
                        CleanTranslationCacheCoroutine(),
                        PostCleanupCoroutine(),
                        CoroutineUtils.CreateCoroutine(() => cleaningActive = false)));
                    //CleanTranslationCache();
                }
                finally
                {
                    //cleaningActive = false;
                }
            }
        }

        private string AutoTranslationsFilePath => GeBoAPI.Instance.AutoTranslationHelper.GetAutoTranslationsFilePath();

        private void ReloadTranslations()
        {
            GeBoAPI.Instance.AutoTranslationHelper.ReloadTranslations();
        }

        private static string GetWorkFileName(string path, string prefix, string extension)
        {
            var timestamp = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            var result = string.Empty;
            for (var i = 0; string.IsNullOrEmpty(result) || File.Exists(result); i++)
            {
                result = Path.Combine(path, string.Join(".", new string[] { prefix, PluginName, timestamp, i.ToString(), extension }));
            }
            return result;
        }

        private static void MoveReplaceFile(string source, string destination)
        {
            string removeFile = null;
            if (File.Exists(destination))
            {
                removeFile = GetWorkFileName(Path.GetDirectoryName(destination), Path.GetFileName(destination), "remove");
                File.Move(destination, removeFile);
            }
            File.Move(source, destination);
            if (removeFile != null)
            {
                File.Delete(removeFile);
            }
        }

        public IEnumerator CleanTranslationCacheCoroutine()
        {
            var reloadCoroutine = CoroutineUtils.CreateCoroutine(() => { }, ReloadTranslations);
            var cutoff = Time.realtimeSinceStartup + YieldSeconds;
            var notifyTime = Time.realtimeSinceStartup + NotifySeconds;
            Logger.LogMessage("Attempting to clean translation cache, please be patient...");
            var cache = GeBoAPI.Instance.AutoTranslationHelper.DefaultCache;

            if (cache == null)
            {
                Logger.LogError("Unable to access translation cache");
                yield break;
            }

            var translations = GeBoAPI.Instance.AutoTranslationHelper.GetTranslations();
            if (translations == null)
            {
                Logger.LogError("Unable to access translation cache");
                yield break;
            }
            
            var regexes = new List<Regex>();

            var tmp = GeBoAPI.Instance.AutoTranslationHelper.GetRegisteredRegexes();//(HashSet<string>)cache?.GetType().GetField("_registeredRegexes", AccessTools.all)?.GetValue(cache);
            if (tmp != null)
            {
                regexes.AddRange(tmp.Select((s) => new Regex(s)));
            }
            tmp = GeBoAPI.Instance.AutoTranslationHelper.GetRegisteredSplitterRegexes();//(HashSet<string>)cache?.GetType().GetField("_registeredSplitterRegexes", AccessTools.all)?.GetValue(cache);
            if (tmp != null)
            {
                regexes.AddRange(tmp.Select((s) => new Regex(s)));
            }

            var newFile = GetWorkFileName(Path.GetDirectoryName(AutoTranslationsFilePath), Path.GetFileName(AutoTranslationsFilePath), "new");
            var backupFile = GetWorkFileName(Path.GetDirectoryName(AutoTranslationsFilePath), Path.GetFileName(AutoTranslationsFilePath), "bak");
            MoveReplaceFile(AutoTranslationsFilePath, backupFile);
            latestBackup = backupFile;
            Logger.LogInfo("Reloading translations without existing cache file");
            yield return StartCoroutine(reloadCoroutine);
            Logger.LogInfo("Reloading done");

            char[] splitter = { '=' };
            var changed = 0;
            using (var outStream = File.Open(newFile, FileMode.CreateNew, FileAccess.Write))
            using (var writer = new StreamWriter(outStream, Encoding.UTF8))
            using (var inStream = File.Open(backupFile, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(inStream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var now = Time.realtimeSinceStartup;
                    if (now > notifyTime)
                    {
                        Logger.LogMessage("Cleaning translation cache...");
                        notifyTime = now + NotifySeconds;
                    }
                    if (now > cutoff)
                    {
                        cutoff = now + YieldSeconds;
                        yield return null;
                    }
                    var parts = line.Split(splitter, StringSplitOptions.None);
                    if (parts.Length == 2 && !parts[0].StartsWith("//", StringComparison.InvariantCulture))
                    {
                        if (translations.ContainsKey(parts[0]))
                        {
                            Logger.LogInfo($"Removing cached line (static match): {line.TrimEnd()}");
                            changed++;
                            continue;
                        }
                        if (regexes.Any((r) => r.IsMatch(parts[0])))
                        {
                            Logger.LogInfo($"Removing cached line (regex match): {line.TrimEnd()}");
                            changed++;
                            continue;
                        }
                    }
                    writer.WriteLine(line);
                }
            }
            yield return null;
            if (changed > 0)
            {
                Logger.LogMessage($"Done. Removed {changed} entries from cache. Reloading translations.");
                MoveReplaceFile(newFile, AutoTranslationsFilePath);
            }
            else
            {
                Logger.LogMessage("Done. No changes made. Restoring/reloading translations");
                MoveReplaceFile(backupFile, AutoTranslationsFilePath);
            }
            latestBackup = null;
            yield return StartCoroutine(reloadCoroutine);
        }

        public IEnumerator PostCleanupCoroutine()
        {
            if (!latestBackup.IsNullOrWhiteSpace() && File.Exists(latestBackup))
            {
                Logger.LogWarning("Something unexpected happened. Restoring previous translation cache.");
                MoveReplaceFile(latestBackup, AutoTranslationsFilePath);
                yield return StartCoroutine(CoroutineUtils.CreateCoroutine(ReloadTranslations));
            }
        }

        public void CleanTranslationCache()
        {
            Logger.LogMessage("Attempting to clean translation cache, please be patient...");
            var cache = GeBoAPI.Instance.AutoTranslationHelper.DefaultCache;

            if (cache == null)
            {
                Logger.LogError("Unable to access translation cache");
                return;
            }

            var translations = GeBoAPI.Instance.AutoTranslationHelper.GetTranslations();
            Logger.LogWarning($"{translations} {translations?.Count}");
            /*
            (Dictionary<string, string>)cache?.GetType().GetField("_translations", AccessTools.all)?.GetValue(cache) ??
            new Dictionary<string, string>();
            */
            var regexes = new List<Regex>();

            var tmp = GeBoAPI.Instance.AutoTranslationHelper.GetRegisteredRegexes();//(HashSet<string>)cache?.GetType().GetField("_registeredRegexes", AccessTools.all)?.GetValue(cache);
            if (tmp != null)
            {
                regexes.AddRange(tmp.Select((s) => new Regex(s)));
            }
            tmp = GeBoAPI.Instance.AutoTranslationHelper.GetRegisteredSplitterRegexes();//(HashSet<string>)cache?.GetType().GetField("_registeredSplitterRegexes", AccessTools.all)?.GetValue(cache);
            if (tmp != null)
            {
                regexes.AddRange(tmp.Select((s) => new Regex(s)));
            }

            var newFile = GetWorkFileName(Path.GetDirectoryName(AutoTranslationsFilePath), Path.GetFileName(AutoTranslationsFilePath), "new");
            var backupFile = GetWorkFileName(Path.GetDirectoryName(AutoTranslationsFilePath), Path.GetFileName(AutoTranslationsFilePath), "bak");
            MoveReplaceFile(AutoTranslationsFilePath, backupFile);
            Logger.LogInfo("Reloading translations without existing cache file");
            ReloadTranslations();

            var completed = false;

            try
            {
                char[] splitter = { '=' };
                var changed = 0;
                using (var outStream = File.Open(newFile, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(outStream, Encoding.UTF8))
                using (var inStream = File.Open(backupFile, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(inStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var parts = line.Split(splitter, StringSplitOptions.None);
                        if (parts.Length == 2 && !parts[0].StartsWith("//", StringComparison.InvariantCulture))
                        {
                            if (translations.ContainsKey(parts[0]))
                            {
                                Logger.LogInfo($"Removing cached line (static match): {line.TrimEnd()}");
                                changed++;
                                continue;
                            }
                            if (regexes.Any((r) => r.IsMatch(parts[0])))
                            {
                                Logger.LogInfo($"Removing cached line (regex match): {line.TrimEnd()}");
                                changed++;
                                continue;
                            }
                        }
                        writer.WriteLine(line);
                    }
                }

                if (changed > 0)
                {
                    Logger.LogMessage($"Done. Removed {changed} entries from cache. Reloading translations.");
                    MoveReplaceFile(newFile, AutoTranslationsFilePath);
                }
                else
                {
                    Logger.LogMessage("Done. No changes made. Restoring/reloading translations");
                    MoveReplaceFile(backupFile, AutoTranslationsFilePath);
                }
                ReloadTranslations();
                completed = true;
            }
            catch (Exception e)
            {
                Logger.LogFatal(e.Message);
                Logger.LogError(e.StackTrace);
                throw;
            }
            finally
            {
                if (!completed)
                {
                    if (File.Exists(backupFile))
                    {
                        Logger.LogWarning("Something unexpected happened. Restoring previous translation cache.");
                        MoveReplaceFile(backupFile, AutoTranslationsFilePath);
                        ReloadTranslations();
                    }
                }
            }
        }
    }
}
