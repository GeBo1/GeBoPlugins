using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using GeBoCommon;
using GeBoCommon.Studio;
using GeBoCommon.Utilities;
using HarmonyLib;
using Illusion.Extensions;
using KKAPI;
using KKAPI.Studio.SaveLoad;
using KKAPI.Utilities;
using Studio;
using UnityEngine;
using KeyboardShortcut = BepInEx.Configuration.KeyboardShortcut;

namespace StudioSceneNavigationPlugin
{
    [BepInDependency(GeBoAPI.GUID, GeBoAPI.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class StudioSceneNavigation : BaseUnityPlugin
    {
        public const string GUID = "com.gebo.bepinex.studioscenenavigation";
        public const string PluginName = "Studio Scene Navigation";
        public const string Version = "0.8.5";

        internal static readonly char[] TrackingFileEntrySplit = { '\0' };

        #region configuration

        public static ConfigEntry<KeyboardShortcut> NavigateNextSceneShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> NavigatePrevSceneShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ReloadCurrentSceneShortcut { get; private set; }
        public static ConfigEntry<bool> NotificationSoundsEnabled { get; private set; }
        public static ConfigEntry<bool> TrackLastLoadedSceneEnabled { get; private set; }

        #endregion configuration

        private static List<string> _scenePaths = new List<string>();
        private static List<string> ScenePaths => _scenePaths;

        private static List<string> _normalizedScenePaths = null;
        private static List<string> NormalizedScenePaths => _normalizedScenePaths ?? (_normalizedScenePaths = new List<string>(ScenePaths.Select(PathUtils.NormalizePath)));

        private static string CurrentSceneFolder = string.Empty;

        private string CurrentScenePathCandidate = string.Empty;
        private string CurrentScenePath = string.Empty;
        private bool NavigationInProgress = false;

        private static readonly object SavePendingLock = new object();
        private static bool setPage = false;
        private static SceneLoadScene sceneLoadScene = null;

        private readonly SimpleLazy<Func<string, bool>> _isSceneValid = null;
        private Func<string, bool> IsSceneValid => _isSceneValid.Value;

        private bool _savePending = false;

        private bool SavePending
        {
            get
            {
                lock (SavePendingLock)
                {
                    return _savePending;
                }
            }
            set
            {
                lock (SavePendingLock)
                {
                    _savePending = value;
                }
            }
        }

        private static readonly string TrackLastLoadedSceneFile = string.Join(Path.DirectorySeparatorChar.ToString(),
            new string[] { "BepinEx", "config",
                string.Join(".", new string[] { GUID, "LastLoadedScene", "data"}) });

        private readonly SimpleLazy<Dictionary<string, string>> _lastLoadedScenes;

        private Dictionary<string, string> LastLoadedScenes => _lastLoadedScenes.Value;

        public StudioSceneNavigation()
        {
            _lastLoadedScenes = new SimpleLazy<Dictionary<string, string>>(() =>
             {
                 if (TrackLastLoadedSceneEnabled.Value)
                 {
                     return LoadTrackingFile();
                 }
                 return new Dictionary<string, string>();
             });

            _isSceneValid = new SimpleLazy<Func<string, bool>>(() =>
            {
                var pluginInfo = Chainloader.PluginInfos.Where((pi) => pi.Key.EndsWith("InvalidSceneFileProtection")).Select((pi) => pi.Value).FirstOrDefault();
                if (pluginInfo != null)
                {
                    var pluginType = pluginInfo.Instance.GetType();
                    var method = AccessTools.Method(pluginType, "IsFileValid");
                    if (method != null)
                    {
                        Logger.LogDebug($"Will use {pluginType.Name}.{method.Name} to pre-check images during navigation");
                        return (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), method);
                    }
                }
                return (_) => true;
            });
        }

        public enum NotificationSound
        {
            Success,
            Error
        }

        internal void Main()
        {
            // TODO: add OnDestroy to verify file write
            NavigateNextSceneShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Next", new KeyboardShortcut(KeyCode.F3, KeyCode.LeftShift), "Navigate to the next (newer) scene");
            NavigatePrevSceneShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Previous", new KeyboardShortcut(KeyCode.F4, KeyCode.LeftShift), "Navigate to the previous (older) scene");
            ReloadCurrentSceneShortcut = Config.Bind("Keyboard Shortcuts", "Reload Current", new KeyboardShortcut(KeyCode.F5, KeyCode.LeftShift), "Reload the currently loaded scene");
            NotificationSoundsEnabled = Config.Bind("Config", "Notification Sounds", true, "When enabled, notification sounds will play when scene loading is complete, or navigation fails");
            TrackLastLoadedSceneEnabled = Config.Bind("Config", "Track Last Loaded Scene", true, "When enabled, the last loaded scene will be tracked externally and can be reloaded upon return");
        }

        internal void Awake()
        {
            if (CurrentSceneFolder.IsNullOrEmpty())
            {
                CurrentSceneFolder = SceneUtils.StudioSceneRootFolder;
            }

            HarmonyWrapper.PatchAll(typeof(StudioSceneNavigation));
            ExtendedSave.SceneBeingLoaded += ExtendedSave_SceneBeingLoaded;
            StudioSaveLoadApi.SceneLoad += StudioSaveLoadApi_SceneLoad;
        }

        internal void Update()
        {
            if (!NavigationInProgress)
            {
                if (NavigateNextSceneShortcut.Value.IsDown())
                {
                    NavigateScene(1);
                }
                else if (NavigatePrevSceneShortcut.Value.IsDown())
                {
                    NavigateScene(-1);
                }
                else if (ReloadCurrentSceneShortcut.Value.IsDown())
                {
                    NavigateScene(0);
                }
            }
        }

        internal void OnDestroy()
        {
            if (TrackLastLoadedSceneEnabled.Value)
            {
                SaveTrackingFile();
            }
        }

        private void StudioSaveLoadApi_SceneLoad(object sender, SceneLoadEventArgs e)
        {
#if DEBUG
            //Logger.Log(LogLevel.Debug, $"StudioSaveLoadApi_SceneLoad({sender}, {e}) {CurrentScenePathCandidate}, {e.Operation}");
#endif
            if (e.Operation == SceneOperationKind.Clear)
            {
                CurrentScenePath = CurrentScenePathCandidate = string.Empty;
            }
            List<IEnumerator> coroutines = new List<IEnumerator>();

            if (!string.IsNullOrEmpty(CurrentScenePathCandidate) && e.Operation == SceneOperationKind.Load)
            {
                CurrentScenePath = CurrentScenePathCandidate;
                CurrentScenePathCandidate = string.Empty;
                if (NavigationInProgress)
                {
                    coroutines.Add(CoroutineUtils.CreateCoroutine(() => PlayNotificationSound(NotificationSound.Success)));
                    coroutines.Add(SetPageCoroutine(CurrentScenePath));
                }
            }
            coroutines.Add(CoroutineUtils.CreateCoroutine(() =>
            {
                NavigationInProgress = false;
                TrackLastLoadedScene();
            }));
            coroutines.Add(SaveTrackingFileCouroutine(1f));
            StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));
        }

        private void ExtendedSave_SceneBeingLoaded(string path)
        {
#if DEBUG
            //Logger.Log(LogLevel.Debug, $"ExtendedSave_SceneBeingLoaded({path})");
#endif
            CurrentScenePathCandidate = PathUtils.NormalizePath(path);
        }

        [HarmonyPatch(typeof(SceneLoadScene), "InitInfo")]
        [HarmonyPostfix]
        public static void StudioInitInfoPost(SceneLoadScene __instance)
        {
#if DEBUG
            //Logger.Log(LogLevel.Debug, $"StudioInitInfoPost({__instance})");
#endif
            CurrentSceneFolder = string.Empty;
            _scenePaths = SceneUtils.GetSceneLoaderPaths(__instance);
            _normalizedScenePaths = null;
            if (ScenePaths.Count > 0)
            {
                CurrentSceneFolder = PathUtils.NormalizePath(Path.GetDirectoryName(ScenePaths[0]));
            }
            sceneLoadScene = __instance;
        }

        private void SaveTrackingFile()
        {
#if DEBUG
            //Logger.Log(LogLevel.Debug, $"SaveTrackingFile fired");
#endif
            var prefix = Path.Combine(BepInEx.Paths.CachePath, Path.GetFileName(TrackLastLoadedSceneFile));
            var newFile = prefix + Path.GetRandomFileName();
            var oldFile = prefix + Path.GetRandomFileName();

            var relativeScenes = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> entry in LastLoadedScenes)
            {
                relativeScenes[PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, entry.Key)] = Path.GetFileName(entry.Value);
            }
            lock (SavePendingLock)
            {
                if (!SavePending)
                {
                    return;
                }

                try
                {
                    using (FileStream fileStream = new FileStream(newFile, FileMode.Create))
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        streamWriter.Write(GUID);
                        streamWriter.Write(TrackingFileEntrySplit[0]);
                        streamWriter.Write(Version);
                        streamWriter.Write(TrackingFileEntrySplit[0]);
                        streamWriter.Write(relativeScenes.Count);
                        streamWriter.Write(TrackingFileEntrySplit[0]);
                        streamWriter.Write('\n');
                        foreach (KeyValuePair<string, string> entry in relativeScenes)
                        {
                            streamWriter.Write(entry.Key);
                            streamWriter.Write(TrackingFileEntrySplit[0]);
                            streamWriter.Write(entry.Value);
                            streamWriter.Write('\n');
                        }
                    }

                    File.Move(TrackLastLoadedSceneFile, oldFile);
                    File.Move(newFile, TrackLastLoadedSceneFile);
                    File.Delete(oldFile);
                    SavePending = false;
                    Logger.Log(LogLevel.Debug, $"Updated {TrackLastLoadedSceneFile}");
                }
                catch (Exception err)
                {
                    if (File.Exists(oldFile))
                    {
                        Logger.Log(LogLevel.Error, $"Error encountered, restoring {TrackLastLoadedSceneFile}");
                        Logger.Log(LogLevel.Error, err);
                        File.Copy(oldFile, TrackLastLoadedSceneFile);
                    }
                    throw;
                }
                finally
                {
                    if (File.Exists(oldFile))
                    {
                        File.Delete(oldFile);
                    }
                    if (File.Exists(newFile))
                    {
                        File.Delete(newFile);
                    }
                }
            }
        }

        private bool TryReadTrackingHeader(StreamReader reader, out Version version, out int expectedCount)
        {
            version = new Version(0, 0, 0, 0);
            expectedCount = -1;

            var line = reader.ReadLine();
            if (!string.IsNullOrEmpty(line))
            {
                var entry = line.Split(TrackingFileEntrySplit);
                if (entry[0] == GUID)
                {
                    try
                    {
                        version = new Version(entry[1]);
                    }
                    catch (ArgumentException) { }
                    catch (FormatException) { }
                    catch (OverflowException) { }

                    if (!int.TryParse(entry[2], out expectedCount))
                    {
                        expectedCount = -1;
                    }

                    return true;
                }
            }
            return false;
        }

        private Dictionary<string, string> LoadTrackingFile()
        {
            
            Dictionary<string, string> lastLoadedScenes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            int count = 0;
            using (FileStream fileStream = new FileStream(TrackLastLoadedSceneFile, FileMode.OpenOrCreate))
            {
                int expectedCount;
                Version version;
                using (StreamReader streamReader = new StreamReader(fileStream, Encoding.UTF8, true))
                {
                    if (!TryReadTrackingHeader(streamReader, out version, out expectedCount))
                    {
                        fileStream.Position = 0;
                    }

                    string line;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.IsNullOrEmpty())
                        {
                            continue;
                        }

                        try
                        {
                            count++;
                            string[] entry = line.Split(TrackingFileEntrySplit, 2);
                            // Normalize on load to take care of any changes between versions
                            if (!Path.IsPathRooted(entry[0]))
                            {
                                if (entry[0] == ".")
                                {
                                    entry[0] = PathUtils.NormalizePath(SceneUtils.StudioSceneRootFolder);
                                }
                                else
                                {
                                    entry[0] = PathUtils.NormalizePath(Path.Combine(SceneUtils.StudioSceneRootFolder,
                                        entry[0]));
                                }
                            }

                            if (lastLoadedScenes.ContainsKey(entry[0]))
                            {
                                Logger.LogWarning($"LoadTrackingFile: line {count}: duplicate key {entry[0]}");
                            }

                            lastLoadedScenes[entry[0]] = Path.GetFileName(entry[1]);
                        }
                        catch (Exception err)
                        {
                            Logger.Log(LogLevel.Error, $"LoadTrackingFile: line {count}: {line.TrimEnd()}\n{err}");
                        }
                    }
                }

                if (expectedCount >= 0 && version >= new Version(0, 8, 7) && expectedCount != count)
                {
                    Logger.Log(LogLevel.Warning | LogLevel.Message,
                        $"{TrackLastLoadedSceneFile} may be corrupted. It contains {count} entries, expected {expectedCount}.");
                }
            }

            return lastLoadedScenes;
        }
        private void NavigateScene(int offset)
        {
            bool navigated = false;
            if (!NavigationInProgress)
            {
                NavigationInProgress = true;
                Logger.Log(LogLevel.Debug, $"Attempting navigate to scene: {offset}");
                try
                {
                    List<string> paths = NormalizedScenePaths;
                    int index = -1;
                    if (!CurrentScenePath.IsNullOrEmpty() && !paths.IsNullOrEmpty())
                    {
                        index = paths.IndexOf(CurrentScenePath);
                    }
                    if (index == -1)
                    {
                        if (!paths.IsNullOrEmpty())
                        {
                            Logger.Log(LogLevel.Info, $"Folder changed, resuming navigation for: {PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, CurrentSceneFolder)}");
                            if (LoadLastLoadedScene())
                            {
                                navigated = true;
                            }
                        }
                    }
                    else
                    {
                        string nextImage = null;
                        while (nextImage is null)
                        {
                            index -= offset;
                            if (index < 0 || index >= paths.Count)
                            {
                                Logger.Log(LogLevel.Info | LogLevel.Message, $"No further scenes to navigate to.");
                                return;
                            }

                            nextImage = paths[index];
                            if (!IsSceneValid(nextImage))
                            {
                                Logger.Log(LogLevel.Warning | LogLevel.Message, $"Skipping invalid scene file: {nextImage}");
                                nextImage = null;
                            }
                        }

                        List<IEnumerator> coroutines = new List<IEnumerator>
                        {
                            CoroutineUtils.CreateCoroutine(
                                () => Logger.Log(LogLevel.Message | LogLevel.Info,
                                    $"Loading scene {paths.Count - index}/{paths.Count} ('{PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, nextImage)}')")),
                            Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(nextImage)
                        };

                        StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));

                        navigated = true;
                    }
                }
                catch (Exception err)
                {
                    Logger.Log(LogLevel.Error, $"Error navigating scene: {err}");
                }
                finally
                {
                    // error encountered during navigation
                    if (!navigated)
                    {
                        NavigationInProgress = false;
                        PlayNotificationSound(NotificationSound.Error);
                    }
                }
                if (navigated)
                {
                    setPage = true;
                }
            }
        }

        private IEnumerator SetPageCoroutine(string scenePath)
        {
            if (setPage)
            {
                setPage = false;
                yield return null;
                int page = NormalizedScenePaths.IndexOf(scenePath) / IMAGES_PER_PAGE;
                if (page >= 0)
                {
                    sceneLoadScene?.GetType().GetField("page", AccessTools.all)?.SetValue(null, page);
                }
            }
        }

        public IEnumerator SaveTrackingFileCouroutine(float delaySeconds = 0)
        {
            if (TrackLastLoadedSceneEnabled.Value)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);

                if (SavePending)
                {
                    SaveTrackingFile();
                }
            }
        }

        private void TrackLastLoadedScene()
        {
            if (!CurrentScenePath.IsNullOrEmpty())
            {
                string key = PathUtils.NormalizePath(Path.GetDirectoryName(CurrentScenePath));
                if (!LastLoadedScenes.TryGetValue(key, out string current))
                {
                    current = string.Empty;
                }

                string currentValue = Path.GetFileName(CurrentScenePath);
                if (!currentValue.Compare(current, StringComparison.InvariantCultureIgnoreCase))
                {
                    LastLoadedScenes[key] = currentValue;
                    SavePending = true;
                }
            }
        }

        /// <summary>Loads the last loaded scene from the currently active folder</summary>
        /// <returns>`true` if scene was loaded, otherwise `false`</returns>
        private bool LoadLastLoadedScene()
        {
            bool navigated = false;
            bool clearNavigation = !NavigationInProgress;
            NavigationInProgress = true;
            try
            {
                if (!LastLoadedScenes.TryGetValue(CurrentSceneFolder, out string nextImage))
                {
                    nextImage = ScenePaths.LastOrDefault();
                }
                if (nextImage != default)
                {
                    nextImage = PathUtils.NormalizePath(Path.Combine(CurrentSceneFolder, nextImage));

                    if (File.Exists(nextImage))
                    {
                        CurrentScenePathCandidate = nextImage;
                        StartCoroutine(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(nextImage));
                        navigated = true;
                    }
                }
            }
            finally
            {
                if (!navigated)
                {
                    Logger.Log(LogLevel.Message | LogLevel.Error, $"Error loading last scene from {CurrentSceneFolder}");
                    if (clearNavigation)
                    {
                        NavigationInProgress = false;
                    }
                }
            }
            return navigated;
        }
    }
}
