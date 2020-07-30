using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
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
using BepInLogLevel = BepInEx.Logging.LogLevel;

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
        public const string Version = "0.8.7";

        internal static readonly char[] TrackingFileEntrySplit = {'\0'};

        private static List<string> _normalizedScenePaths;

        private static string _currentSceneFolder = string.Empty;

        private static readonly object SavePendingLock = new object();
        private static bool _setPage;
        private static SceneLoadScene _sceneLoadScene;


        private static readonly string TrackLastLoadedSceneFile = PathUtils.CombinePaths(
            "BepInEx", "config", StringUtils.JoinStrings(".", GUID, "LastLoadedScene", "data"));

        private readonly SimpleLazy<Func<string, bool>> _isSceneValid;

        private readonly SimpleLazy<Dictionary<string, string>> _lastLoadedScenes;
        private string _currentScenePath = string.Empty;

        private string _currentScenePathCandidate = string.Empty;
        private bool _navigationInProgress;

        private bool _savePending;

        public StudioSceneNavigation()
        {
            _lastLoadedScenes = new SimpleLazy<Dictionary<string, string>>(() =>
                TrackLastLoadedSceneEnabled.Value ? LoadTrackingFile() : new Dictionary<string, string>());

            _isSceneValid = new SimpleLazy<Func<string, bool>>(() =>
            {
                var pluginInfo = Chainloader.PluginInfos.Where(pi => pi.Key.EndsWith("InvalidSceneFileProtection"))
                    .Select(pi => pi.Value).FirstOrDefault();
                if (pluginInfo == null) return _ => true;

                var pluginType = pluginInfo.Instance.GetType();
                var method = AccessTools.Method(pluginType, "IsFileValid");
                if (method == null) return _ => true;

                Logger.LogDebug(
                    $"Will use {pluginType.Name}.{method.Name} to pre-check images during navigation");
                return (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), method);
            });
        }

        private static List<string> ScenePaths { get; set; } = new List<string>();

        private static List<string> NormalizedScenePaths => _normalizedScenePaths ??
                                                            (_normalizedScenePaths =
                                                                new List<string>(
                                                                    ScenePaths.Select(PathUtils.NormalizePath)));

        private Func<string, bool> IsSceneValid => _isSceneValid.Value;

        private bool SavePending
        {
            get
            {
                lock (SavePendingLock) return _savePending;
            }
            set
            {
                lock (SavePendingLock) _savePending = value;
            }
        }

        private Dictionary<string, string> LastLoadedScenes => _lastLoadedScenes.Value;

        public static ConfigEntry<KeyboardShortcut> NavigateNextSceneShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> NavigatePrevSceneShortcut { get; private set; }
        public static ConfigEntry<KeyboardShortcut> ReloadCurrentSceneShortcut { get; private set; }
        public static ConfigEntry<bool> NotificationSoundsEnabled { get; private set; }
        public static ConfigEntry<bool> TrackLastLoadedSceneEnabled { get; private set; }

        internal void Main()
        {
            NavigateNextSceneShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Next",
                new KeyboardShortcut(KeyCode.F3, KeyCode.LeftShift), "Navigate to the next (newer) scene");
            NavigatePrevSceneShortcut = Config.Bind("Keyboard Shortcuts", "Navigate Previous",
                new KeyboardShortcut(KeyCode.F4, KeyCode.LeftShift), "Navigate to the previous (older) scene");
            ReloadCurrentSceneShortcut = Config.Bind("Keyboard Shortcuts", "Reload Current",
                new KeyboardShortcut(KeyCode.F5, KeyCode.LeftShift), "Reload the currently loaded scene");
            NotificationSoundsEnabled = Config.Bind("Config", "Notification Sounds", true,
                "When enabled, notification sounds will play when scene loading is complete, or navigation fails");
            TrackLastLoadedSceneEnabled = Config.Bind("Config", "Track Last Loaded Scene", true,
                "When enabled, the last loaded scene will be tracked externally and can be reloaded upon return");
            GeBoAPI.Instance.SetupNotificationSoundConfig(GUID, NotificationSoundsEnabled);
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Unity")]
        internal void Awake()
        {
            if (_currentSceneFolder.IsNullOrEmpty()) _currentSceneFolder = SceneUtils.StudioSceneRootFolder;

            Harmony.CreateAndPatchAll(typeof(Hooks));
            ExtendedSave.SceneBeingLoaded += ExtendedSave_SceneBeingLoaded;
            StudioSaveLoadApi.SceneLoad += StudioSaveLoadApi_SceneLoad;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Unity")]
        internal void Update()
        {
            if (_navigationInProgress) return;

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

        [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Unity")]
        internal void OnDestroy()
        {
            if (TrackLastLoadedSceneEnabled.Value)
            {
                SaveTrackingFile();
            }
        }

        private void PlayNotificationSound(NotificationSound notificationSound)
        {
            GeBoAPI.Instance.PlayNotificationSound(notificationSound, GUID);
        }

        private void StudioSaveLoadApi_SceneLoad(object sender, SceneLoadEventArgs e)
        {
#if DEBUG
            //Logger.LogDebug( $"StudioSaveLoadApi_SceneLoad({sender}, {e}) {CurrentScenePathCandidate}, {e.Operation}");
#endif
            if (e.Operation == SceneOperationKind.Clear)
            {
                _currentScenePath = _currentScenePathCandidate = string.Empty;
            }

            var coroutines = new List<IEnumerator>();

            if (!string.IsNullOrEmpty(_currentScenePathCandidate) && e.Operation == SceneOperationKind.Load)
            {
                _currentScenePath = _currentScenePathCandidate;
                _currentScenePathCandidate = string.Empty;
                if (_navigationInProgress)
                {
                    coroutines.Add(
                        CoroutineUtils.CreateCoroutine(() => PlayNotificationSound(NotificationSound.Success)));
                    coroutines.Add(SetPageCoroutine(_currentScenePath));
                }
            }

            coroutines.Add(CoroutineUtils.CreateCoroutine(() =>
            {
                _navigationInProgress = false;
                TrackLastLoadedScene();
            }));
            coroutines.Add(SaveTrackingFileCouroutine(1f));
            StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));
        }

        private void ExtendedSave_SceneBeingLoaded(string path)
        {
#if DEBUG
            //Logger.LogDebug( $"ExtendedSave_SceneBeingLoaded({path})");
#endif
            _currentScenePathCandidate = PathUtils.NormalizePath(path);
        }


        private void SaveTrackingFile()
        {
#if DEBUG
            //Logger.LogDebug( $"SaveTrackingFile fired");
#endif
            if (TrackLastLoadedSceneFile == null)
            {
                throw new NullReferenceException($"{nameof(TrackLastLoadedSceneFile)} should not be null");
            }

            var prefix = Path.Combine(Paths.CachePath, Path.GetFileName(TrackLastLoadedSceneFile));
            var newFile = prefix + Path.GetRandomFileName();
            var oldFile = prefix + Path.GetRandomFileName();

            var relativeScenes = new Dictionary<string, string>();
            foreach (var entry in LastLoadedScenes)
            {
                relativeScenes[PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, entry.Key)] =
                    Path.GetFileName(entry.Value);
            }

            lock (SavePendingLock)
            {
                if (!SavePending) return;

                try
                {
                    using (var fileStream = new FileStream(newFile, FileMode.Create))
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        streamWriter.Write(GUID);
                        streamWriter.Write(TrackingFileEntrySplit[0]);
                        streamWriter.Write(Version);
                        streamWriter.Write(TrackingFileEntrySplit[0]);
                        streamWriter.Write(relativeScenes.Count);
                        streamWriter.Write(TrackingFileEntrySplit[0]);
                        streamWriter.Write('\n');
                        foreach (var entry in relativeScenes)
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
                    Logger.LogDebug($"Updated {TrackLastLoadedSceneFile}");
                }
                catch (Exception err)
                {
                    if (!File.Exists(oldFile)) throw;
                    Logger.LogError($"Error encountered, restoring {TrackLastLoadedSceneFile}: {err.Message}");
                    Logger.DebugLogDebug(err);

                    File.Copy(oldFile, TrackLastLoadedSceneFile);

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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Crash protection")]
        private bool TryReadTrackingHeader(StreamReader reader, out Version version, out int expectedCount)
        {
            version = new Version(0, 0, 0, 0);
            expectedCount = -1;


            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            var entry = line.Split(TrackingFileEntrySplit);
            if (entry[0] != GUID)
            {
                return false;
            }

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


        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Crash protection")]
        private Dictionary<string, string> LoadTrackingFile()
        {
            var lastLoadedScenes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            var count = 0;
            using (var fileStream = new FileStream(TrackLastLoadedSceneFile, FileMode.OpenOrCreate))
            {
                int expectedCount;
                Version version;
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true))
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
                            var entry = line.Split(TrackingFileEntrySplit, 2);
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
                            Logger.LogError($"LoadTrackingFile: line {count}: {line.TrimEnd()}\n{err}");
                        }
                    }
                }

                if (expectedCount >= 0 && version >= new Version(0, 8, 7) && expectedCount != count)
                {
                    Logger.Log(BepInLogLevel.Warning | BepInLogLevel.Message,
                        $"{TrackLastLoadedSceneFile} may be corrupted. It contains {count} entries, expected {expectedCount}.");
                }
            }

            return lastLoadedScenes;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Crash protection")]
        private void NavigateScene(int offset)
        {
            if (_navigationInProgress) return;

            var navigated = false;
            _navigationInProgress = true;
            Logger.LogDebug($"Attempting navigate to scene: {offset}");
            try
            {
                var paths = NormalizedScenePaths;
                var index = -1;
                if (!_currentScenePath.IsNullOrEmpty() && !paths.IsNullOrEmpty())
                {
                    index = paths.IndexOf(_currentScenePath);
                }

                if (index == -1)
                {
                    if (!paths.IsNullOrEmpty())
                    {
                        Logger.LogInfo(
                            $"Folder changed, resuming navigation for: {PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, _currentSceneFolder)}");
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
                            Logger.Log(BepInLogLevel.Info | BepInLogLevel.Message,
                                "No further scenes to navigate to.");
                            return;
                        }

                        nextImage = paths[index];
                        if (IsSceneValid(nextImage)) continue;

                        Logger.Log(BepInLogLevel.Warning | BepInLogLevel.Message,
                            $"Skipping invalid scene file: {nextImage}");
                        nextImage = null;
                    }

                    var coroutines = new List<IEnumerator>
                    {
                        CoroutineUtils.CreateCoroutine(
                            () => Logger.Log(BepInLogLevel.Message | BepInLogLevel.Info,
                                $"Loading scene {paths.Count - index}/{paths.Count} ('{PathUtils.GetRelativePath(SceneUtils.StudioSceneRootFolder, nextImage)}')")),
                        Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(nextImage)
                    };

                    StartCoroutine(CoroutineUtils.ComposeCoroutine(coroutines.ToArray()));

                    navigated = true;
                }
            }
            catch (Exception err)
            {
                Logger.LogError($"Error navigating scene: {err}");
            }
            finally
            {
                // error encountered during navigation
                if (!navigated)
                {
                    _navigationInProgress = false;
                    PlayNotificationSound(NotificationSound.Error);
                }
            }

            if (navigated) _setPage = true;
        }

        private IEnumerator SetPageCoroutine(string scenePath)
        {
            if (!_setPage) yield break;

            _setPage = false;
            yield return null;
            var page = NormalizedScenePaths.IndexOf(scenePath) / ImagesPerPage;
            if (page < 0) yield break;

            _sceneLoadScene?.GetType().GetField("page", AccessTools.all)?.SetValue(null, page);
        }

        public IEnumerator SaveTrackingFileCouroutine(float delaySeconds = 0)
        {
            if (!TrackLastLoadedSceneEnabled.Value) yield break;
            yield return new WaitForSecondsRealtime(delaySeconds);

            if (SavePending) SaveTrackingFile();
        }

        private void TrackLastLoadedScene()
        {
            if (_currentScenePath.IsNullOrEmpty()) return;
            var key = PathUtils.NormalizePath(Path.GetDirectoryName(_currentScenePath));
            if (!LastLoadedScenes.TryGetValue(key, out var current)) current = string.Empty;
            var currentValue = Path.GetFileName(_currentScenePath);
            if (currentValue.Compare(current, StringComparison.InvariantCultureIgnoreCase)) return;
            LastLoadedScenes[key] = currentValue;
            SavePending = true;
        }

        /// <summary>Loads the last loaded scene from the currently active folder</summary>
        /// <returns>`true` if scene was loaded, otherwise `false`</returns>
        private bool LoadLastLoadedScene()
        {
            var navigated = false;
            var clearNavigation = !_navigationInProgress;
            _navigationInProgress = true;
            try
            {
                if (!LastLoadedScenes.TryGetValue(_currentSceneFolder, out var nextImage))
                {
                    nextImage = ScenePaths.LastOrDefault();
                }

                if (nextImage != default)
                {
                    nextImage = PathUtils.NormalizePath(Path.Combine(_currentSceneFolder, nextImage));

                    if (File.Exists(nextImage))
                    {
                        _currentScenePathCandidate = nextImage;
                        StartCoroutine(Singleton<Studio.Studio>.Instance.LoadSceneCoroutine(nextImage));
                        navigated = true;
                    }
                }
            }
            finally
            {
                if (!navigated)
                {
                    Logger.Log(BepInLogLevel.Message | BepInLogLevel.Error,
                        $"Error loading last scene from {_currentSceneFolder}");
                    if (clearNavigation)
                    {
                        _navigationInProgress = false;
                    }
                }
            }

            return navigated;
        }
    }
}
